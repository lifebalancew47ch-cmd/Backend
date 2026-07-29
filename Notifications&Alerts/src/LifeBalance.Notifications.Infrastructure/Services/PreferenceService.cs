using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class PreferenceService : IPreferenceService
{
    private readonly MongoDbContext _db;

    public PreferenceService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationPreferenceDto> GetAsync(string userId)
    {
        var filter = Builders<NotificationPreference>.Filter.Eq(p => p.UserId, userId);
        var pref = await _db.NotificationPreferences.Find(filter).FirstOrDefaultAsync();

        if (pref is null)
        {
            pref = new NotificationPreference { UserId = userId };
            await _db.NotificationPreferences.InsertOneAsync(pref);
        }

        return MapToDto(pref);
    }

    public async Task<NotificationPreferenceDto> UpdateAsync(string userId, UpdatePreferenceDto dto)
    {
        var filter = Builders<NotificationPreference>.Filter.Eq(p => p.UserId, userId);
        var pref = await _db.NotificationPreferences.Find(filter).FirstOrDefaultAsync();

        if (pref is null)
        {
            pref = new NotificationPreference
            {
                UserId = userId,
                ReceivePush = dto.ReceivePush ?? true,
                ReceiveWearOS = dto.ReceiveWearOS ?? true,
                ReceiveEmail = dto.ReceiveEmail ?? true,
                ReceiveSedentaryAlerts = dto.ReceiveSedentaryAlerts ?? true,
                ReceiveMarketing = dto.ReceiveMarketing ?? true,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.NotificationPreferences.InsertOneAsync(pref);
        }
        else
        {
            if (dto.ReceivePush.HasValue) pref.ReceivePush = dto.ReceivePush.Value;
            if (dto.ReceiveWearOS.HasValue) pref.ReceiveWearOS = dto.ReceiveWearOS.Value;
            if (dto.ReceiveEmail.HasValue) pref.ReceiveEmail = dto.ReceiveEmail.Value;
            if (dto.ReceiveSedentaryAlerts.HasValue) pref.ReceiveSedentaryAlerts = dto.ReceiveSedentaryAlerts.Value;
            if (dto.ReceiveMarketing.HasValue) pref.ReceiveMarketing = dto.ReceiveMarketing.Value;
            pref.UpdatedAt = DateTime.UtcNow;

            await _db.NotificationPreferences.ReplaceOneAsync(filter, pref);
        }

        return MapToDto(pref);
    }

    private static NotificationPreferenceDto MapToDto(NotificationPreference p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        ReceivePush = p.ReceivePush,
        ReceiveWearOS = p.ReceiveWearOS,
        ReceiveEmail = p.ReceiveEmail,
        ReceiveSedentaryAlerts = p.ReceiveSedentaryAlerts,
        ReceiveMarketing = p.ReceiveMarketing,
        UpdatedAt = p.UpdatedAt
    };
}
