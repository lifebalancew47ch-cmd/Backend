using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class HistoryService : IHistoryService
{
    private readonly MongoDbContext _db;

    public HistoryService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<NotificationHistoryDto>> GetAllAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
        var notifications = await _db.Notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(n => new NotificationHistoryDto
        {
            Id = n.Id,
            Title = n.Title,
            Body = n.Body,
            Type = n.Type,
            Channel = n.Channel,
            Status = n.Status,
            CreatedAt = n.CreatedAt,
            SentAt = n.SentAt
        }).ToList();
    }

    public async Task<Notification?> GetByIdAsync(string id)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
        return await _db.Notifications.Find(filter).FirstOrDefaultAsync();
    }
}
