using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class IngestionServiceClient : IIngestionServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IngestionServiceClient> _logger;

    public IngestionServiceClient(HttpClient httpClient, ILogger<IngestionServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<IngestionHistoryItemDto>?> GetHistoryAsync(string? type, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var pageSizeClamped = Math.Clamp(pageSize, 1, 100);
        var pageClamped = Math.Max(page, 1);
        try
        {
            var query = $"/api/v1/ingestion/history?page={pageClamped}&pageSize={pageSizeClamped}";
            if (!string.IsNullOrWhiteSpace(type))
            {
                query += $"&type={Uri.EscapeDataString(type)}";
            }

            return await _httpClient.GetWrappedAsync<List<IngestionHistoryItemDto>>(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve ingestion history (type: {Type}).", type);
            return null;
        }
    }

    public async Task<IngestionSyncBatchStatusDto?> GetSyncBatchAsync(string clientBatchId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetWrappedAsync<IngestionSyncBatchStatusDto>(
                $"/api/v1/ingestion/sync/{Uri.EscapeDataString(clientBatchId)}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve ingestion batch {ClientBatchId}.", clientBatchId);
            return null;
        }
    }
}