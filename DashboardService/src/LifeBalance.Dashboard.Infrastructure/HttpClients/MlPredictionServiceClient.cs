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

    public async Task<List<RecommendationDto>> GetRecommendationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<RecommendationDto>>($"/api/v1/ml/recommendations/{userId}", cancellationToken);
            return res ?? GetFallbackRecommendations();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve ML recommendations for UserId: {UserId}", userId);
            return GetFallbackRecommendations();
        }
    }

    public async Task<HealthRiskTrendDto?> GetHealthRiskTrendAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<HealthRiskTrendDto>($"/api/v1/ml/risk-trend/{userId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve ML health risk trend for UserId: {UserId}", userId);
            return new HealthRiskTrendDto(userId, "Low", 0.15, new List<string> { "Maintain 8000 daily steps", "Take 5 min walk every 2 hours" });
        }
    }

    private static List<RecommendationDto> GetFallbackRecommendations() => new()
    {
        new RecommendationDto("rec_1", "Posture", "Take a posture check break", "Standing up for 2 minutes reduces lumbar strain.", 0.95),
        new RecommendationDto("rec_2", "Hydration", "Drink 250ml of water", "Optimal hydration boosts energy levels during work hours.", 0.88)
    };
}
