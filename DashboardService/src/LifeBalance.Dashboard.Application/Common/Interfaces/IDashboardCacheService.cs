namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public interface IDashboardCacheService
{
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string cacheKey, T data, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default);
}
