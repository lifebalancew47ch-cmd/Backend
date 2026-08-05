using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using LifeBalance.OrganizationSaaS.Application.Interfaces;

namespace LifeBalance.OrganizationSaaS.Infrastructure.Services;

public class TenantContextAccessor : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return string.Empty;

            // 1. JWT claim 'tenant_id' always takes precedence over any header
            var tenantClaim = context.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrWhiteSpace(tenantClaim))
            {
                return tenantClaim;
            }

            // 2. X-Tenant-Id header is trusted only as a fallback for authenticated users
            if (context.User?.Identity?.IsAuthenticated == true
                && context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
                && !string.IsNullOrWhiteSpace(tenantHeader))
            {
                return tenantHeader.ToString();
            }

            return string.Empty;
        }
    }

    public string? OrganizationId => _httpContextAccessor.HttpContext?.User?.FindFirst("organization_id")?.Value;

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string CorrelationId => _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public DistributedCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(data)) return default;

        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
        };

        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }
}
