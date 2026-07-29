using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly MongoDbContext _db;

    public EmailService(MongoDbContext db) { _db = db; }

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
            Status = NotificationStatus.Sent, CreatedAt = DateTime.UtcNow, SentAt = DateTime.UtcNow, Attempts = 1
        };
        await _db.Notifications.InsertOneAsync(notification);
        await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = notification.Id, UserId = dto.To, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, Attempts = 1, Provider = "Smtp" });
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

        var notification = new Notification { UserId = string.Join(",", dto.To), Title = template.Subject, Body = body, Type = template.Type, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, CreatedAt = DateTime.UtcNow, SentAt = DateTime.UtcNow, Attempts = 1 };
        await _db.Notifications.InsertOneAsync(notification);
        foreach (var r in dto.To)
            await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = notification.Id, UserId = r, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, Attempts = 1, Provider = "Smtp" });
        return Map(notification);
    }

    public async Task<List<NotificationResponseDto>> SendBulkAsync(BulkEmailDto dto)
    {
        var results = new List<NotificationResponseDto>();
        foreach (var r in dto.To)
        {
            var n = new Notification { UserId = r, Title = dto.Subject, Body = dto.Body, Type = NotificationType.Information, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, CreatedAt = DateTime.UtcNow, SentAt = DateTime.UtcNow, Attempts = 1 };
            await _db.Notifications.InsertOneAsync(n);
            await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog { NotificationId = n.Id, UserId = r, Channel = NotificationChannel.Email, Status = NotificationStatus.Sent, Attempts = 1, Provider = "Smtp" });
            results.Add(Map(n));
        }
        return results;
    }

    private static NotificationResponseDto Map(Notification n) => new() { Id = n.Id, UserId = n.UserId, Title = n.Title, Body = n.Body, Type = n.Type, Channel = n.Channel, Status = n.Status, CreatedAt = n.CreatedAt, SentAt = n.SentAt, Attempts = n.Attempts };
}
