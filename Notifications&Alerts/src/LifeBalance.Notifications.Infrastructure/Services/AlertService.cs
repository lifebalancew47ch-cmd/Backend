using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly MongoDbContext _db;

    public AlertService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<AlertDto> CreateAsync(CreateAlertDto dto)
    {
        var alert = new Alert
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Body = dto.Body,
            Source = dto.Source,
            Priority = dto.Priority,
            IsRead = false,
            IsDismissed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Alerts.InsertOneAsync(alert);

        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Body = dto.Body,
            Type = MapPriorityToType(dto.Priority),
            Channel = NotificationChannel.Push,
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow
        };

        await _db.Notifications.InsertOneAsync(notification);

        return MapToDto(alert);
    }

    public async Task<List<AlertDto>> GetAllAsync(string userId)
    {
        var filter = Builders<Alert>.Filter.Eq(a => a.UserId, userId);
        var alerts = await _db.Alerts.Find(filter)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync();

        return alerts.Select(MapToDto).ToList();
    }

    public async Task<AlertDto?> GetByIdAsync(string id)
    {
        var filter = Builders<Alert>.Filter.Eq(a => a.Id, id);
        var alert = await _db.Alerts.Find(filter).FirstOrDefaultAsync();
        return alert is null ? null : MapToDto(alert);
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        var filter = Builders<Alert>.Filter.Eq(a => a.Id, id);
        var update = Builders<Alert>.Update
            .Set(a => a.IsRead, true)
            .Set(a => a.ReadAt, DateTime.UtcNow);

        var result = await _db.Alerts.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DismissAsync(string id)
    {
        var filter = Builders<Alert>.Filter.Eq(a => a.Id, id);
        var update = Builders<Alert>.Update.Set(a => a.IsDismissed, true);
        var result = await _db.Alerts.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    private static NotificationType MapPriorityToType(AlertPriority priority) => priority switch
    {
        AlertPriority.Critical => NotificationType.Warning,
        AlertPriority.Preventive => NotificationType.Warning,
        AlertPriority.Informational => NotificationType.Information,
        AlertPriority.Reminder => NotificationType.Reminder,
        AlertPriority.Compliance => NotificationType.Compliance,
        AlertPriority.Goals => NotificationType.Goals,
        AlertPriority.Inactivity => NotificationType.Inactivity,
        AlertPriority.SedentaryRisk => NotificationType.SedentaryRisk,
        AlertPriority.Organizational => NotificationType.Organizational,
        _ => NotificationType.Information
    };

    private static AlertDto MapToDto(Alert a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        Title = a.Title,
        Body = a.Body,
        Source = a.Source,
        Priority = a.Priority,
        IsRead = a.IsRead,
        IsDismissed = a.IsDismissed,
        CreatedAt = a.CreatedAt,
        ReadAt = a.ReadAt
    };
}
