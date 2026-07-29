using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly MongoDbContext _db;

    public NotificationService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationResponseDto> SendAsync(SendNotificationDto dto)
    {
        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Body = dto.Body,
            Payload = dto.Payload,
            Type = dto.Type,
            Channel = dto.Channel,
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow
        };

        await _db.Notifications.InsertOneAsync(notification);

        await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog
        {
            NotificationId = notification.Id,
            UserId = dto.UserId,
            Channel = dto.Channel,
            Status = NotificationStatus.Sent,
            Attempts = 1
        });

        return MapToResponse(notification);
    }

    public async Task<List<NotificationResponseDto>> BroadcastAsync(BroadcastNotificationDto dto)
    {
        var results = new List<NotificationResponseDto>();

        foreach (var userId in dto.UserIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = dto.Title,
                Body = dto.Body,
                Payload = dto.Payload,
                Type = dto.Type,
                Channel = dto.Channel,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow
            };

            await _db.Notifications.InsertOneAsync(notification);

            await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog
            {
                NotificationId = notification.Id,
                UserId = userId,
                Channel = dto.Channel,
                Status = NotificationStatus.Sent,
                Attempts = 1
            });

            results.Add(MapToResponse(notification));
        }

        return results;
    }

    private static NotificationResponseDto MapToResponse(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Title = n.Title,
        Body = n.Body,
        Payload = n.Payload,
        Type = n.Type,
        Channel = n.Channel,
        Status = n.Status,
        CreatedAt = n.CreatedAt,
        SentAt = n.SentAt
    };
}
