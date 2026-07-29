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
    public AlertService(MongoDbContext db) { _db = db; }

    public async Task<AlertDto> CreateAsync(CreateAlertDto dto)
    {
        var alert = new Alert { UserId = dto.UserId, Title = dto.Title, Body = dto.Body, Source = dto.Source, Priority = dto.Priority, CreatedAt = DateTime.UtcNow };
        await _db.Alerts.InsertOneAsync(alert);
        var notification = new Notification { UserId = dto.UserId, Title = dto.Title, Body = dto.Body, Type = MapPriority(dto.Priority), Channel = NotificationChannel.Push, Status = NotificationStatus.Sent, CreatedAt = DateTime.UtcNow, SentAt = DateTime.UtcNow };
        await _db.Notifications.InsertOneAsync(notification);
        return Map(alert);
    }

    public async Task<List<AlertDto>> GetAllAsync(string userId)
    {
        var alerts = await _db.Alerts.Find(a => a.UserId == userId).SortByDescending(a => a.CreatedAt).ToListAsync();
        return alerts.Select(Map).ToList();
    }

    public async Task<AlertDto?> GetByIdAsync(string id)
    {
        var alert = await _db.Alerts.Find(a => a.Id == id).FirstOrDefaultAsync();
        return alert is null ? null : Map(alert);
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        var r = await _db.Alerts.UpdateOneAsync(a => a.Id == id, Builders<Alert>.Update.Set(a => a.IsRead, true).Set(a => a.ReadAt, DateTime.UtcNow));
        return r.ModifiedCount > 0;
    }

    public async Task<bool> DismissAsync(string id)
    {
        var r = await _db.Alerts.UpdateOneAsync(a => a.Id == id, Builders<Alert>.Update.Set(a => a.IsDismissed, true));
        return r.ModifiedCount > 0;
    }

    private static NotificationType MapPriority(AlertPriority p) => p switch
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

    private static AlertDto Map(Alert a) => new() { Id = a.Id, UserId = a.UserId, Title = a.Title, Body = a.Body, Source = a.Source, Priority = a.Priority, IsRead = a.IsRead, IsDismissed = a.IsDismissed, CreatedAt = a.CreatedAt, ReadAt = a.ReadAt };
}
