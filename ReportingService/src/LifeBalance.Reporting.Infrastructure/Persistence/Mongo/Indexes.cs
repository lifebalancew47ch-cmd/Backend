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
    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var logs = database.GetCollection<ReportGenerationLog>(DomainConstants.ReportLogsCollection);

        await logs.Indexes.CreateOneAsync(
            new CreateIndexModel<ReportGenerationLog>(
                Builders<ReportGenerationLog>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Descending(x => x.TimestampUtc)),
            cancellationToken: cancellationToken);

        await logs.Indexes.CreateOneAsync(
            new CreateIndexModel<ReportGenerationLog>(
                Builders<ReportGenerationLog>.IndexKeys
                    .Ascending(x => x.Scope)
                    .Ascending(x => x.ScopeId),
                new CreateIndexOptions { Name = "scope_scopeid" }),
            cancellationToken: cancellationToken);
    }
}
