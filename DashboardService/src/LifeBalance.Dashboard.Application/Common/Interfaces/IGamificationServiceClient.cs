namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record UserRewardsResponseDto(string UserId, int Points, int BadgesUnlocked, int CurrentStreakDays, List<string> RecentRewards);
public record ChallengeProgressDto(string ChallengeId, string Title, double ProgressPercentage, bool Completed);

public interface IGamificationServiceClient
{
    Task<UserRewardsResponseDto?> GetUserRewardsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<ChallengeProgressDto>> GetFamilyChallengesAsync(string familyId, CancellationToken cancellationToken = default);
}
