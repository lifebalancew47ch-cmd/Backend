using LifeBalance.Dashboard.Domain.Entities;

namespace LifeBalance.Dashboard.Domain.Repositories;

public interface IDashboardCacheRepository
{
    Task<DashboardCacheEntry?> GetByKeyAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task SetAsync(DashboardCacheEntry entry, CancellationToken cancellationToken = default);
    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default);
}
