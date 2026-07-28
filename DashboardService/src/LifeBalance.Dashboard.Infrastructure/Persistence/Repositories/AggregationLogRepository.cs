using LifeBalance.Dashboard.Domain.Entities;
using LifeBalance.Dashboard.Domain.Repositories;
using LifeBalance.Dashboard.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace LifeBalance.Dashboard.Infrastructure.Persistence.Repositories;

public class AggregationLogRepository : IAggregationLogRepository
{
    private readonly IMongoCollection<AggregationLog> _collection;

    public AggregationLogRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<AggregationLog>("AggregationLogs");
    }

    public async Task AddLogAsync(AggregationLog log, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(log, null, cancellationToken);
    }

    public async Task<IEnumerable<AggregationLog>> GetRecentLogsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(Builders<AggregationLog>.Filter.Empty)
            .SortByDescending(x => x.TimestampUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }
}
