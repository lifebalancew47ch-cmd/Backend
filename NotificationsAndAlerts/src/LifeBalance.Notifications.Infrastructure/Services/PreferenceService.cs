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
        if (pref is null) pref = new NotificationPreference { UserId = userId };

        if (dto.ReceivePush.HasValue) pref.ReceivePush = dto.ReceivePush.Value;
        if (dto.ReceiveEmail.HasValue) pref.ReceiveEmail = dto.ReceiveEmail.Value;
        if (dto.ReceiveSms.HasValue) pref.ReceiveSms = dto.ReceiveSms.Value;
        if (dto.ReceiveWearOS.HasValue) pref.ReceiveWearOS = dto.ReceiveWearOS.Value;
        if (dto.ReceiveCriticalAlerts.HasValue) pref.ReceiveCriticalAlerts = dto.ReceiveCriticalAlerts.Value;
        if (dto.ReceiveReminders.HasValue) pref.ReceiveReminders = dto.ReceiveReminders.Value;
        if (dto.ReceiveGoals.HasValue) pref.ReceiveGoals = dto.ReceiveGoals.Value;
        if (dto.ReceiveGamification.HasValue) pref.ReceiveGamification = dto.ReceiveGamification.Value;
        if (dto.ReceiveOrganizational.HasValue) pref.ReceiveOrganizational = dto.ReceiveOrganizational.Value;
        if (dto.AllowedStartTime is not null) pref.AllowedStartTime = TimeSpan.Parse(dto.AllowedStartTime);
        if (dto.AllowedEndTime is not null) pref.AllowedEndTime = TimeSpan.Parse(dto.AllowedEndTime);
        if (dto.QuietModeEnabled.HasValue) pref.QuietModeEnabled = dto.QuietModeEnabled.Value;
        if (dto.QuietModeStart is not null) pref.QuietModeStart = TimeSpan.Parse(dto.QuietModeStart);
        if (dto.QuietModeEnd is not null) pref.QuietModeEnd = TimeSpan.Parse(dto.QuietModeEnd);
        if (dto.Frequency is not null) pref.Frequency = dto.Frequency;
        if (dto.Language is not null) pref.Language = dto.Language;
        if (dto.Timezone is not null) pref.Timezone = dto.Timezone;
        pref.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(pref.Id))
            await _db.NotificationPreferences.InsertOneAsync(pref);
        else
            await _db.NotificationPreferences.ReplaceOneAsync(filter, pref);

        return MapToDto(pref);
    }

    public async Task<NotificationPreferenceDto> UpdatePushAsync(string userId, bool enabled)
        => await UpdateAsync(userId, new UpdatePreferenceDto { ReceivePush = enabled });

    public async Task<NotificationPreferenceDto> UpdateEmailAsync(string userId, bool enabled)
        => await UpdateAsync(userId, new UpdatePreferenceDto { ReceiveEmail = enabled });

    public async Task<NotificationPreferenceDto> UpdateWearOSAsync(string userId, bool enabled)
        => await UpdateAsync(userId, new UpdatePreferenceDto { ReceiveWearOS = enabled });

    private static NotificationPreferenceDto MapToDto(NotificationPreference p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        ReceivePush = p.ReceivePush,
        ReceiveEmail = p.ReceiveEmail,
        ReceiveSms = p.ReceiveSms,
        ReceiveWearOS = p.ReceiveWearOS,
        ReceiveCriticalAlerts = p.ReceiveCriticalAlerts,
        ReceiveReminders = p.ReceiveReminders,
        ReceiveGoals = p.ReceiveGoals,
        ReceiveGamification = p.ReceiveGamification,
        ReceiveOrganizational = p.ReceiveOrganizational,
        AllowedStartTime = p.AllowedStartTime?.ToString(),
        AllowedEndTime = p.AllowedEndTime?.ToString(),
        QuietModeEnabled = p.QuietModeEnabled,
        QuietModeStart = p.QuietModeStart?.ToString(),
        QuietModeEnd = p.QuietModeEnd?.ToString(),
        Frequency = p.Frequency,
        Language = p.Language,
        Timezone = p.Timezone,
        UpdatedAt = p.UpdatedAt
    };
}
