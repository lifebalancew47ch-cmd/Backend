using LifeBalance.Reporting.Infrastructure.Persistence.Mongo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.Services;

/// <summary>
/// Ensures the MongoDB indexes exist at startup without blocking request processing
/// or the health checks. Failures are logged and never crash the service.
/// </summary>
public sealed class MongoIndexInitializer : BackgroundService
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<MongoIndexInitializer> _logger;

    /// <summary>Initializes a new instance of <see cref="MongoIndexInitializer"/>.</summary>
    public MongoIndexInitializer(MongoDbContext dbContext, ILogger<MongoIndexInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _dbContext.EnsureIndexesAsync(stoppingToken);
            _logger.LogInformation("MongoDB indexes ensured at startup.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown in progress — nothing to report.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure MongoDB indexes at startup. Indexes will be missing until the next restart.");
        }
    }
}
