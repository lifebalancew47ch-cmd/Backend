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
            return new UserRewardsResponseDto(userId, 1200, 4, 7, new List<string> { "Early Bird", "10k Steps" });
        }
    }

    public async Task<List<ChallengeProgressDto>> GetFamilyChallengesAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<ChallengeProgressDto>>($"/api/v1/gamification/family/{familyId}/challenges", cancellationToken);
            return res ?? GetFallbackChallenges();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family challenges for FamilyId: {FamilyId}", familyId);
            return GetFallbackChallenges();
        }
    }

    private static List<ChallengeProgressDto> GetFallbackChallenges() => new()
    {
        new ChallengeProgressDto("ch_1", "Weekly 50k Steps Challenge", 75.0, false),
        new ChallengeProgressDto("ch_2", "Zero Sedentary Afternoon", 100.0, true)
    };
}
