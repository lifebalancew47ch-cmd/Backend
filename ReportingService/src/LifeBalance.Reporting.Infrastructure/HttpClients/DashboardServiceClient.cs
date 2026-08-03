using System.Net.Http.Json;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.HttpClients;

/// <summary>
/// Implementation of <see cref="IDashboardServiceClient"/> using a typed <see cref="HttpClient"/>.
/// Returns <c>null</c> on failure so callers fail closed (503).
/// </summary>
public sealed class DashboardServiceClient : IDashboardServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardServiceClient> _logger;

    /// <summary>Initializes a new instance of <see cref="DashboardServiceClient"/>.</summary>
    public DashboardServiceClient(HttpClient httpClient, ILogger<DashboardServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DashboardKpisDto?> GetKpisAsync(string scope, string? scopeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = scopeId is null
                ? $"/api/v1/dashboard/{scope}/kpis"
                : $"/api/v1/dashboard/{scope}/kpis?{scope}Id={scopeId}";
            return await _httpClient.GetFromJsonAsync<DashboardKpisDto>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve KPIs for scope {Scope} with id {ScopeId}", scope, scopeId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync(string scope, string? scopeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = scopeId is null
                ? $"/api/v1/dashboard/{scope}/summary"
                : $"/api/v1/dashboard/{scope}/summary?{scope}Id={scopeId}";
            return await _httpClient.GetFromJsonAsync<DashboardSummaryDto>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve dashboard summary for scope {Scope} with id {ScopeId}", scope, scopeId);
            return null;
        }
    }
}
