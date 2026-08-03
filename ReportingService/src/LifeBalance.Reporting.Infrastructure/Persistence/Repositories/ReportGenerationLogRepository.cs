using LifeBalance.Reporting.Domain.Constants;
using LifeBalance.Reporting.Domain.Entities;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.Repositories;
using LifeBalance.Reporting.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace LifeBalance.Reporting.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB implementation of <see cref="IReportGenerationLogRepository"/>.
/// </summary>
public sealed class ReportGenerationLogRepository : IReportGenerationLogRepository
{
    private readonly IMongoCollection<ReportGenerationLog> _collection;

    /// <summary>Initializes a new instance of <see cref="ReportGenerationLogRepository"/>.</summary>
    public ReportGenerationLogRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<ReportGenerationLog>(DomainConstants.ReportLogsCollection);
    }

    /// <inheritdoc/>
    public async Task AddAsync(ReportGenerationLog log, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(log, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<ReportGenerationLog> Items, int Total)> GetByUserAsync(
        string userId,
        int pageIndex,
        int pageSize,
        ReportScope? scope = null,
        ReportFormat? format = null,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<ReportGenerationLog>>
        {
            Builders<ReportGenerationLog>.Filter.Eq(x => x.UserId, userId)
        };

        if (scope.HasValue)
        {
            filters.Add(Builders<ReportGenerationLog>.Filter.Eq(x => x.Scope, scope.Value));
        }

        if (format.HasValue)
        {
            filters.Add(Builders<ReportGenerationLog>.Filter.Eq(x => x.Format, format.Value));
        }

        var filter = filters.Count == 1 ? filters[0] : Builders<ReportGenerationLog>.Filter.And(filters);

        var total = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await _collection
            .Find(filter)
            .SortByDescending(x => x.TimestampUtc)
            .Skip(pageIndex * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
