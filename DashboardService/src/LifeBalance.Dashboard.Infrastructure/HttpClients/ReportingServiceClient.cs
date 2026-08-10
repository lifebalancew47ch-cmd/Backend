using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class ReportingServiceClient : IReportingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReportingServiceClient> _logger;

    public ReportingServiceClient(HttpClient httpClient, ILogger<ReportingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeneralSystemMetricsDto?> GetSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<ReportingApiResponse<GeneralSystemMetricsDto>>("/api/v1/reports/system-metrics", cancellationToken);
            return res?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve global system metrics");
            // DevSecOps Rule 5 (fail-closed): never fabricate metrics. Returning null makes
            // GeneralDashboardQueryHandlers throw UpstreamServiceUnavailableException (503)
            // instead of masking a Reporting outage with a fake 99.9% "Healthy".
            return null;
        }
    }

    private sealed class ReportingApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
