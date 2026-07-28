namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record RecommendationDto(string RecommendationId, string Category, string Title, string Description, double PriorityScore);
public record HealthRiskTrendDto(string UserId, string RiskLevel, double SedentaryRiskScore, List<string> RecommendedActions);

public interface IMlPredictionServiceClient
{
    Task<List<RecommendationDto>> GetRecommendationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<HealthRiskTrendDto?> GetHealthRiskTrendAsync(string userId, CancellationToken cancellationToken = default);
}
