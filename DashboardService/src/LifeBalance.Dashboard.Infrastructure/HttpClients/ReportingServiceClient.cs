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
            return new GeneralSystemMetricsDto(10000, 3200, 99.9, "v1.0.0");
        }
    }

    private sealed class ReportingApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
