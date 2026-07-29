using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly MongoDbContext _db;

    public TemplateService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<TemplateDto>> GetAllAsync()
    {
        var templates = await _db.NotificationTemplates.Find(_ => true).ToListAsync();
        return templates.Select(MapToDto).ToList();
    }

    public async Task<TemplateDto> CreateAsync(CreateTemplateDto dto)
    {
        var template = new NotificationTemplate
        {
            Name = dto.Name,
            Subject = dto.Subject,
            BodyContent = dto.BodyContent,
            Type = dto.Type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.NotificationTemplates.InsertOneAsync(template);
        return MapToDto(template);
    }

    public async Task<TemplateDto?> UpdateAsync(string id, CreateTemplateDto dto)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.Id, id);
        var template = await _db.NotificationTemplates.Find(filter).FirstOrDefaultAsync();
        if (template is null) return null;

        template.Name = dto.Name;
        template.Subject = dto.Subject;
        template.BodyContent = dto.BodyContent;
        template.Type = dto.Type;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.NotificationTemplates.ReplaceOneAsync(filter, template);
        return MapToDto(template);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.Id, id);
        var result = await _db.NotificationTemplates.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    private static TemplateDto MapToDto(NotificationTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Subject = t.Subject,
        BodyContent = t.BodyContent,
        Type = t.Type,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
