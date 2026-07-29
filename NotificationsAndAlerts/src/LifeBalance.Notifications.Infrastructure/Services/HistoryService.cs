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

    public async Task<PaginatedResult<NotificationHistoryDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var totalCount = await _db.Notifications.CountDocumentsAsync(_ => true);
        var notifications = await _db.Notifications.Find(_ => true)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PaginatedResult<NotificationHistoryDto>
        {
            Items = notifications.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<NotificationHistoryDto>> GetByUserAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
        var notifications = await _db.Notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .ToListAsync();
        return notifications.Select(MapToDto).ToList();
    }

    public async Task<List<NotificationHistoryDto>> GetByOrganizationAsync(string organizationId)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.OrganizationId, organizationId);
        var notifications = await _db.Notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .ToListAsync();
        return notifications.Select(MapToDto).ToList();
    }

    public async Task<NotificationResponseDto?> GetByIdAsync(string id)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
        var notification = await _db.Notifications.Find(filter).FirstOrDefaultAsync();
        if (notification is null) return null;
        return new NotificationResponseDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            OrganizationId = notification.OrganizationId,
            FamilyId = notification.FamilyId,
            DepartmentId = notification.DepartmentId,
            Title = notification.Title,
            Body = notification.Body,
            Payload = notification.Payload,
            Type = notification.Type,
            Channel = notification.Channel,
            Status = notification.Status,
            IsRead = notification.IsRead,
            IsArchived = notification.IsArchived,
            IsFavorite = notification.IsFavorite,
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt,
            ReadAt = notification.ReadAt,
            DeliveryTimeMs = notification.DeliveryTimeMs,
            Attempts = notification.Attempts,
            ErrorMessage = notification.ErrorMessage,
            Provider = notification.Provider
        };
    }

    private static NotificationHistoryDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        OrganizationId = n.OrganizationId,
        FamilyId = n.FamilyId,
        DepartmentId = n.DepartmentId,
        Title = n.Title,
        Body = n.Body,
        Type = n.Type,
        Channel = n.Channel,
        Status = n.Status,
        IsRead = n.IsRead,
        IsArchived = n.IsArchived,
        IsFavorite = n.IsFavorite,
        DeliveryTimeMs = n.DeliveryTimeMs,
        Attempts = n.Attempts,
        ErrorMessage = n.ErrorMessage,
        Provider = n.Provider,
        CreatedAt = n.CreatedAt,
        SentAt = n.SentAt,
        ReadAt = n.ReadAt
    };
}
