using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class DeviceRegistrationService : IDeviceRegistrationService
{
    private readonly MongoDbContext _db;
    public DeviceRegistrationService(MongoDbContext db) { _db = db; }

    public async Task RegisterAsync(DeviceRegistrationDto dto)
    {
        var filter = Builders<DeviceRegistration>.Filter.And(
            Builders<DeviceRegistration>.Filter.Eq(d => d.UserId, dto.UserId),
            Builders<DeviceRegistration>.Filter.Eq(d => d.DeviceToken, dto.DeviceToken)
        );
        var existing = await _db.DeviceRegistrations.Find(filter).FirstOrDefaultAsync();
        if (existing != null)
        {
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Platform = dto.Platform;
            await _db.DeviceRegistrations.ReplaceOneAsync(filter, existing);
            return;
        }
        await _db.DeviceRegistrations.InsertOneAsync(new DeviceRegistration
        {
            UserId = dto.UserId,
            DeviceToken = dto.DeviceToken,
            Platform = dto.Platform,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<List<string>> GetDeviceTokensAsync(string userId)
    {
        var filter = Builders<DeviceRegistration>.Filter.And(
            Builders<DeviceRegistration>.Filter.Eq(d => d.UserId, userId),
            Builders<DeviceRegistration>.Filter.Eq(d => d.IsActive, true)
        );
        var devices = await _db.DeviceRegistrations.Find(filter).ToListAsync();
        return devices.Select(d => d.DeviceToken).Distinct().ToList();
    }

    public async Task<bool> UnregisterAsync(string userId, string deviceToken)
    {
        var filter = Builders<DeviceRegistration>.Filter.And(
            Builders<DeviceRegistration>.Filter.Eq(d => d.UserId, userId),
            Builders<DeviceRegistration>.Filter.Eq(d => d.DeviceToken, deviceToken)
        );
        var result = await _db.DeviceRegistrations.UpdateOneAsync(filter,
            Builders<DeviceRegistration>.Update.Set(d => d.IsActive, false).Set(d => d.UpdatedAt, DateTime.UtcNow));
        return result.ModifiedCount > 0;
    }
}
