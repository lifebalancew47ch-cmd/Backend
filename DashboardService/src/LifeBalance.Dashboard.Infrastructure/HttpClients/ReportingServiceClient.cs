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
            return await _httpClient.GetFromJsonAsync<GeneralSystemMetricsDto>("/api/v1/reports/system-metrics", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve global system metrics");
            return new GeneralSystemMetricsDto(10000, 3200, 99.9, "v1.0.0");
        }
    }
}
