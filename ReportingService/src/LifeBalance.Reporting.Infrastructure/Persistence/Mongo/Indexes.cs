using LifeBalance.Reporting.Domain.Constants;
using LifeBalance.Reporting.Domain.Entities;
using MongoDB.Driver;

namespace LifeBalance.Reporting.Infrastructure.Persistence.Mongo;

/// <summary>
/// Creates the MongoDB indexes required by the Reporting service collections.
/// </summary>
internal static class Indexes
{
    /// <summary>Ensures all required indexes exist. Idempotent.</summary>
    public static void EnsureIndexes(IMongoDatabase database)
    {
        var logs = database.GetCollection<ReportGenerationLog>(DomainConstants.ReportLogsCollection);

        logs.Indexes.CreateOne(
            new CreateIndexModel<ReportGenerationLog>(
                Builders<ReportGenerationLog>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Descending(x => x.TimestampUtc)));

        logs.Indexes.CreateOne(
            new CreateIndexModel<ReportGenerationLog>(
                Builders<ReportGenerationLog>.IndexKeys
                    .Ascending(x => x.Scope)
                    .Ascending(x => x.ScopeId),
                new CreateIndexOptions { Name = "scope_scopeid" }));
    }
}
