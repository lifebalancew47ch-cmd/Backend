using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

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
            SentAt = DateTime.UtcNow,
            Attempts = 1
        };

        await _db.Notifications.InsertOneAsync(notification);

        await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog
        {
            NotificationId = notification.Id,
            UserId = dto.UserId,
            Channel = dto.Channel,
            Status = NotificationStatus.Sent,
            Attempts = 1,
            Provider = "System"
        });

        return MapToResponse(notification);
    }

    public async Task<List<NotificationResponseDto>> SendBulkAsync(List<SendNotificationDto> dtos)
    {
        var results = new List<NotificationResponseDto>();

        foreach (var dto in dtos)
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
                SentAt = DateTime.UtcNow,
                Attempts = 1
            };

            await _db.Notifications.InsertOneAsync(notification);

            await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog
            {
                NotificationId = notification.Id,
                UserId = dto.UserId,
                Channel = dto.Channel,
                Status = NotificationStatus.Sent,
                Attempts = 1,
                Provider = "System"
            });

            results.Add(MapToResponse(notification));
        }

        return results;
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
                SentAt = DateTime.UtcNow,
                Attempts = 1
            };

            await _db.Notifications.InsertOneAsync(notification);

            await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog
            {
                NotificationId = notification.Id,
                UserId = userId,
                Channel = dto.Channel,
                Status = NotificationStatus.Sent,
                Attempts = 1,
                Provider = "System"
            });

            results.Add(MapToResponse(notification));
        }

        return results;
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

        return MapToResponse(notification);
    }

    public async Task<List<NotificationResponseDto>> GetAllAsync(string? userId = null, string? organizationId = null, string? familyId = null, string? departmentId = null)
    {
        var filters = new List<FilterDefinition<Notification>>();

        if (!string.IsNullOrEmpty(userId))
            filters.Add(Builders<Notification>.Filter.Eq(n => n.UserId, userId));
        if (!string.IsNullOrEmpty(organizationId))
            filters.Add(Builders<Notification>.Filter.Eq(n => n.OrganizationId, organizationId));
        if (!string.IsNullOrEmpty(familyId))
            filters.Add(Builders<Notification>.Filter.Eq(n => n.FamilyId, familyId));
        if (!string.IsNullOrEmpty(departmentId))
            filters.Add(Builders<Notification>.Filter.Eq(n => n.DepartmentId, departmentId));

        var filter = filters.Count > 0
            ? Builders<Notification>.Filter.And(filters)
            : Builders<Notification>.Filter.Empty;

        var notifications = await _db.Notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToResponse).ToList();
    }

    public async Task<NotificationResponseDto?> GetByIdAsync(string id)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
        var notification = await _db.Notifications.Find(filter).FirstOrDefaultAsync();
        return notification is null ? null : MapToResponse(notification);
    }

    public async Task<bool> CancelAsync(string id)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.Id, id),
            Builders<Notification>.Filter.Eq(n => n.Status, NotificationStatus.Pending)
        );

        var update = Builders<Notification>.Update.Set(n => n.Status, NotificationStatus.Cancelled);
        var result = await _db.Notifications.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow);

        var result = await _db.Notifications.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.UserId, userId),
            Builders<Notification>.Filter.Eq(n => n.IsRead, false)
        );

        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow);

        var result = await _db.Notifications.UpdateManyAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> ArchiveAsync(string id)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
        var update = Builders<Notification>.Update.Set(n => n.IsArchived, true);
        var result = await _db.Notifications.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> FavoriteAsync(string id)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
        var notification = await _db.Notifications.Find(filter).FirstOrDefaultAsync();
        if (notification is null) return false;

        var update = Builders<Notification>.Update.Set(n => n.IsFavorite, !notification.IsFavorite);
        var result = await _db.Notifications.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
        var result = await _db.Notifications.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
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
        IsRead = n.IsRead,
        IsArchived = n.IsArchived,
        IsFavorite = n.IsFavorite,
        CreatedAt = n.CreatedAt,
        SentAt = n.SentAt,
        ReadAt = n.ReadAt,
        DeliveryTimeMs = n.DeliveryTimeMs,
        Attempts = n.Attempts,
        ErrorMessage = n.ErrorMessage,
        Provider = n.Provider
    };
}
