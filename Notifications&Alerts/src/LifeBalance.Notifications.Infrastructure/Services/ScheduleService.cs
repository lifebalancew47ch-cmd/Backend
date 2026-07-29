using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly MongoDbContext _db;

    public ScheduleService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationResponseDto> ScheduleAsync(ScheduleNotificationDto dto)
    {
        var scheduled = new ScheduledNotification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Body = dto.Body,
            Payload = dto.Payload,
            Type = dto.Type,
            Channel = dto.Channel,
            ScheduledFor = dto.ScheduledFor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _db.ScheduledNotifications.InsertOneAsync(scheduled);

        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Body = dto.Body,
            Payload = dto.Payload,
            Type = dto.Type,
            Channel = dto.Channel,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Notifications.InsertOneAsync(notification);

        return new NotificationResponseDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Body = notification.Body,
            Payload = notification.Payload,
            Type = notification.Type,
            Channel = notification.Channel,
            Status = notification.Status,
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt
        };
    }

    public async Task<bool> CancelAsync(string id)
    {
        var filter = Builders<ScheduledNotification>.Filter.And(
            Builders<ScheduledNotification>.Filter.Eq(s => s.Id, id),
            Builders<ScheduledNotification>.Filter.Eq(s => s.IsActive, true)
        );

        var update = Builders<ScheduledNotification>.Update.Set(s => s.IsActive, false);
        var result = await _db.ScheduledNotifications.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }
}
