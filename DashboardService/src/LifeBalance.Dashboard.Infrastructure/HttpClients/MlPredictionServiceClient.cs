using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class MlPredictionServiceClient : IMlPredictionServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MlPredictionServiceClient> _logger;

    public MlPredictionServiceClient(HttpClient httpClient, ILogger<MlPredictionServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<RecommendationDto>?> GetRecommendationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetWrappedAsync<List<RecommendationDto>>($"/api/v1/ml/recommendations/{userId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve ML recommendations for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<HealthRiskTrendDto?> GetHealthRiskTrendAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetWrappedAsync<HealthRiskTrendDto>($"/api/v1/ml/risk-trend/{userId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve ML health risk trend for UserId: {UserId}", userId);
            return null;
        }
    }
}
