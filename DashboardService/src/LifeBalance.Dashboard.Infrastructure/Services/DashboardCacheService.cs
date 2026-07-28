using System.Text.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Domain.Entities;
using LifeBalance.Dashboard.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.Services;

public class DashboardCacheService : IDashboardCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDashboardCacheRepository _mongoCacheRepo;
    private readonly ILogger<DashboardCacheService> _logger;

    public DashboardCacheService(
        IMemoryCache memoryCache,
        IDashboardCacheRepository mongoCacheRepo,
        ILogger<DashboardCacheService> logger)
    {
        _memoryCache = memoryCache;
        _mongoCacheRepo = mongoCacheRepo;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(cacheKey, out T? cachedValue))
        {
            _logger.LogDebug("Cache HIT (MemoryCache) for Key: {CacheKey}", cacheKey);
            return cachedValue;
        }

        try
        {
            var mongoEntry = await _mongoCacheRepo.GetByKeyAsync(cacheKey, cancellationToken);
            if (mongoEntry != null && !string.IsNullOrEmpty(mongoEntry.PayloadJson))
            {
                var deserialized = JsonSerializer.Deserialize<T>(mongoEntry.PayloadJson);
                if (deserialized != null)
                {
                    _logger.LogDebug("Cache HIT (MongoDB) for Key: {CacheKey}", cacheKey);
                    _memoryCache.Set(cacheKey, deserialized, mongoEntry.ExpiresAtUtc - DateTime.UtcNow);
                    return deserialized;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading cache key {CacheKey} from MongoDB persistence", cacheKey);
        }

        _logger.LogDebug("Cache MISS for Key: {CacheKey}", cacheKey);
        return default;
    }

    public async Task SetAsync<T>(string cacheKey, T data, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (data == null) return;

        _memoryCache.Set(cacheKey, data, expiration);

        try
        {
            var entry = new DashboardCacheEntry
            {
                CacheKey = cacheKey,
                PayloadJson = JsonSerializer.Serialize(data),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.Add(expiration)
            };
            await _mongoCacheRepo.SetAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache key {CacheKey} in MongoDB persistence", cacheKey);
        }
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(cacheKey);

        try
        {
            await _mongoCacheRepo.RemoveAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache key {CacheKey} from MongoDB persistence", cacheKey);
        }
    }
}
