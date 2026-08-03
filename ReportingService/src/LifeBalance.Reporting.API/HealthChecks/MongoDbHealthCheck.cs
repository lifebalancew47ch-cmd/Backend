using LifeBalance.Reporting.Infrastructure.Persistence.Mongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LifeBalance.Reporting.API.HealthChecks;

/// <summary>
/// Reports MongoDB connectivity without ever throwing. A missing/unreachable Mongo
/// is reported as <see cref="HealthCheckResult.Unhealthy"/> (HTTP 503) instead of
/// surfacing an unhandled 500 on the health endpoint.
/// </summary>
public sealed class MongoDbHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    /// <summary>Initializes a new instance of <see cref="MongoDbHealthCheck"/>.</summary>
    public MongoDbHealthCheck(IServiceProvider serviceProvider, ILogger<MongoDbHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dbContext = _serviceProvider.GetRequiredService<MongoDbContext>();
            using var cursor = await dbContext.Client.ListDatabaseNamesAsync(cancellationToken);
            await cursor.MoveNextAsync(cancellationToken);

            return HealthCheckResult.Healthy("MongoDB is reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB health check failed.");
            return HealthCheckResult.Unhealthy("MongoDB is unreachable.", ex);
        }
    }
}
