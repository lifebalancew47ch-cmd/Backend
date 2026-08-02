using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class GamificationServiceClient : IGamificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GamificationServiceClient> _logger;

    public GamificationServiceClient(HttpClient httpClient, ILogger<GamificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserRewardsResponseDto?> GetUserRewardsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserRewardsResponseDto>($"/api/v1/gamification/user/{userId}/rewards", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve rewards for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<List<ChallengeProgressDto>?> GetFamilyChallengesAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ChallengeProgressDto>>($"/api/v1/gamification/family/{familyId}/challenges", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family challenges for FamilyId: {FamilyId}", familyId);
            return null;
        }
    }
}
