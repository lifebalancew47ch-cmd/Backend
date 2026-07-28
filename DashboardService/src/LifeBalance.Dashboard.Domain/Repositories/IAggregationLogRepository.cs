using LifeBalance.Dashboard.Domain.Entities;

namespace LifeBalance.Dashboard.Domain.Repositories;

public interface IAggregationLogRepository
{
    Task AddLogAsync(AggregationLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<AggregationLog>> GetRecentLogsAsync(int limit = 100, CancellationToken cancellationToken = default);
}
