using System.Net;
using System.Net.Mail;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Configuration;
using LifeBalance.Notifications.Infrastructure.Data;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly MongoDbContext _db;
    private readonly SmtpSettings _smtp;

    public EmailService(MongoDbContext db, IOptions<SmtpSettings> smtp)
    {
        _db = db;
        _smtp = smtp.Value;
    }

    public async Task<NotificationResponseDto> SendAsync(SendEmailDto dto)
    {
        var body = dto.Body;
        if (!string.IsNullOrEmpty(dto.TemplateId) && dto.TemplateVariables != null)
        {
            var template = await _db.NotificationTemplates.Find(t => t.Id == dto.TemplateId).FirstOrDefaultAsync();
            if (template != null)
            {
                body = template.BodyContent;
                foreach (var v in dto.TemplateVariables)
                    body = body.Replace($"{{{{{v.Key}}}}}", v.Value);
            }
        }

        var notification = new Notification
        {
            UserId = dto.To, Title = dto.Subject, Body = body,
            Type = NotificationType.Information, Channel = NotificationChannel.Email,
            Status = NotificationStatus.Pending, CreatedAt = DateTime.UtcNow
        };
        await _db.Notifications.InsertOneAsync(notification);

        try
        {
            await SendSmtpAsync(dto.To, dto.Subject, body);
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            notification.Attempts = 1;
            await _db.Notifications.ReplaceOneAsync(n => n.Id == notification.Id, notification);
            await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = notification.Id, UserId = dto.To, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, Attempts = 1, Provider = "Smtp" });
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
            notification.Attempts = 1;
            await _db.Notifications.ReplaceOneAsync(n => n.Id == notification.Id, notification);
            await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = notification.Id, UserId = dto.To, Channel = NotificationChannel.Email, Status = NotificationStatus.Failed, Attempts = 1, Provider = "Smtp", ErrorMessage = ex.Message });

            throw;
        }

        return Map(notification);
    }

    public async Task<NotificationResponseDto> SendTemplateAsync(EmailTemplateDto dto)
    {
        var template = await _db.NotificationTemplates.Find(t => t.Id == dto.TemplateId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {dto.TemplateId} not found");
        var body = template.BodyContent;
        if (dto.Variables != null)
            foreach (var v in dto.Variables)
                body = body.Replace($"{{{{{v.Key}}}}}", v.Value);

        var notification = new Notification
        {
            UserId = string.Join(",", dto.To), Title = template.Subject, Body = body,
            Type = template.Type, Channel = NotificationChannel.Email,
            Status = NotificationStatus.Pending, CreatedAt = DateTime.UtcNow
        };
        await _db.Notifications.InsertOneAsync(notification);

        try
        {
            foreach (var r in dto.To)
                await SendSmtpAsync(r, template.Subject, body);

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            notification.Attempts = 1;
            await _db.Notifications.ReplaceOneAsync(n => n.Id == notification.Id, notification);
            foreach (var r in dto.To)
                await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = notification.Id, UserId = r, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, Attempts = 1, Provider = "Smtp" });
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
            await _db.Notifications.ReplaceOneAsync(n => n.Id == notification.Id, notification);
            throw;
        }

        return Map(notification);
    }

    public async Task<List<NotificationResponseDto>> SendBulkAsync(BulkEmailDto dto)
    {
        var results = new List<NotificationResponseDto>();
        var errors = new List<string>();

        foreach (var r in dto.To)
        {
            var n = new Notification
            {
                UserId = r, Title = dto.Subject, Body = dto.Body,
                Type = NotificationType.Information, Channel = NotificationChannel.Email,
                Status = NotificationStatus.Pending, CreatedAt = DateTime.UtcNow
            };
            await _db.Notifications.InsertOneAsync(n);

            try
            {
                await SendSmtpAsync(r, dto.Subject, dto.Body);
                n.Status = NotificationStatus.Sent;
                n.SentAt = DateTime.UtcNow;
                n.Attempts = 1;
                await _db.Notifications.ReplaceOneAsync(x => x.Id == n.Id, n);
                await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = n.Id, UserId = r, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, Attempts = 1, Provider = "Smtp" });
            }
            catch (Exception ex)
            {
                n.Status = NotificationStatus.Failed;
                n.ErrorMessage = ex.Message;
                n.Attempts = 1;
                await _db.Notifications.ReplaceOneAsync(x => x.Id == n.Id, n);
                await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = n.Id, UserId = r, Channel = NotificationChannel.Email, Status = NotificationStatus.Failed, Attempts = 1, Provider = "Smtp", ErrorMessage = ex.Message });
                errors.Add($"{r}: {ex.Message}");
            }

            results.Add(Map(n));
        }

        if (errors.Count > 0)
            throw new InvalidOperationException($"SMTP errors: {string.Join("; ", errors)}");

        return results;
    }

    private async Task SendSmtpAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
            EnableSsl = _smtp.EnableSsl
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message);
    }

    private static NotificationResponseDto Map(Notification n) =>
        new()
        {
            Id = n.Id, UserId = n.UserId, Title = n.Title, Body = n.Body,
            Type = n.Type, Channel = n.Channel, Status = n.Status,
            CreatedAt = n.CreatedAt, SentAt = n.SentAt, Attempts = n.Attempts
        };
}
