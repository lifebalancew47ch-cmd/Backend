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

    public async Task<TemplateDto?> GetByIdAsync(string id)
    {
        var filter = Builders<NotificationTemplate>.Filter.Eq(t => t.Id, id);
        var template = await _db.NotificationTemplates.Find(filter).FirstOrDefaultAsync();
        return template is null ? null : MapToDto(template);
    }

    public async Task<TemplateDto> CreateAsync(CreateTemplateDto dto)
    {
        var template = new NotificationTemplate
        {
            Name = dto.Name,
            Subject = dto.Subject,
            BodyContent = dto.BodyContent,
            HtmlContent = dto.HtmlContent,
            Type = dto.Type,
            Channel = dto.Channel,
            Variables = dto.Variables,
            Version = 1,
            IsGlobal = dto.IsGlobal,
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

        template.Version++;
        template.Name = dto.Name;
        template.Subject = dto.Subject;
        template.BodyContent = dto.BodyContent;
        template.HtmlContent = dto.HtmlContent;
        template.Type = dto.Type;
        template.Channel = dto.Channel;
        template.Variables = dto.Variables;
        template.IsGlobal = dto.IsGlobal;
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
        HtmlContent = t.HtmlContent,
        Type = t.Type,
        Channel = t.Channel,
        Variables = t.Variables,
        Version = t.Version,
        IsGlobal = t.IsGlobal,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
