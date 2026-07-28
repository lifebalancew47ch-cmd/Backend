using LifeBalance.Dashboard.Domain.Entities;
using LifeBalance.Dashboard.Domain.Repositories;
using LifeBalance.Dashboard.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace LifeBalance.Dashboard.Infrastructure.Persistence.Repositories;

public class DashboardCacheRepository : IDashboardCacheRepository
{
    private readonly IMongoCollection<DashboardCacheEntry> _collection;

    public DashboardCacheRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<DashboardCacheEntry>("DashboardCache");
    }

    public async Task<DashboardCacheEntry?> GetByKeyAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        var filter = Builders<DashboardCacheEntry>.Filter.Eq(x => x.CacheKey, cacheKey);
        var entry = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (entry != null && entry.IsExpired)
        {
            await RemoveAsync(cacheKey, cancellationToken);
            return null;
        }
        return entry;
    }

    public async Task SetAsync(DashboardCacheEntry entry, CancellationToken cancellationToken = default)
    {
        var filter = Builders<DashboardCacheEntry>.Filter.Eq(x => x.CacheKey, entry.CacheKey);
        await _collection.ReplaceOneAsync(filter, entry, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        var filter = Builders<DashboardCacheEntry>.Filter.Eq(x => x.CacheKey, cacheKey);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }
}
