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

    public async Task<NotificationResponseDto> ScheduleAsync(ScheduleRequestDto dto)
    {
        var scheduled = new ScheduledNotification
        {
            UserId = dto.UserId,
            OrganizationId = dto.OrganizationId,
            FamilyId = dto.FamilyId,
            DepartmentId = dto.DepartmentId,
            Title = dto.Title,
            Body = dto.Body,
            Payload = dto.Payload,
            Type = dto.Type,
            Channel = dto.Channel,
            ScheduledFor = dto.ScheduledFor,
            Recurrence = dto.Recurrence,
            RepeatInterval = dto.RepeatInterval,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _db.ScheduledNotifications.InsertOneAsync(scheduled);

        var notification = new Notification
        {
            UserId = dto.UserId,
            OrganizationId = dto.OrganizationId,
            FamilyId = dto.FamilyId,
            DepartmentId = dto.DepartmentId,
            Title = dto.Title,
            Body = dto.Body,
            Payload = dto.Payload,
            Type = dto.Type,
            Channel = dto.Channel,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Notifications.InsertOneAsync(notification);

        return MapToResponse(notification);
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

    public async Task<bool> RescheduleAsync(string id, DateTime newScheduledFor)
    {
        var filter = Builders<ScheduledNotification>.Filter.And(
            Builders<ScheduledNotification>.Filter.Eq(s => s.Id, id),
            Builders<ScheduledNotification>.Filter.Eq(s => s.IsActive, true)
        );

        var update = Builders<ScheduledNotification>.Update.Set(s => s.ScheduledFor, newScheduledFor);
        var result = await _db.ScheduledNotifications.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<List<NotificationResponseDto>> GetScheduledAsync(string? userId = null)
    {
        var filter = string.IsNullOrEmpty(userId)
            ? Builders<ScheduledNotification>.Filter.Empty
            : Builders<ScheduledNotification>.Filter.Eq(s => s.UserId, userId);

        var scheduled = await _db.ScheduledNotifications.Find(filter)
            .SortBy(s => s.ScheduledFor)
            .ToListAsync();

        return scheduled.Select(s => new NotificationResponseDto
        {
            Id = s.Id,
            UserId = s.UserId,
            Title = s.Title,
            Body = s.Body,
            Payload = s.Payload,
            Type = s.Type,
            Channel = s.Channel,
            Status = NotificationStatus.Pending,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    private static NotificationResponseDto MapToResponse(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        OrganizationId = n.OrganizationId,
        FamilyId = n.FamilyId,
        DepartmentId = n.DepartmentId,
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
