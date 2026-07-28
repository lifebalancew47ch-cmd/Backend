using LifeBalance.Dashboard.Application.Common.Interfaces;

namespace LifeBalance.Dashboard.Application.Features.FamilyDashboard;

public record FamilyDashboardResponse(
    string FamilyId,
    List<AuthUserResponseDto> Members,
    List<MedicalDataResponseDto> FamilyBiometrics,
    List<ChallengeProgressDto> Challenges
);

public record FamilyStatisticsResponse(string FamilyId, int MemberCount, int TotalFamilySteps, double AverageActiveMinutes);
public record FamilyGoalsResponse(string FamilyId, List<ChallengeProgressDto> ActiveGoals);
public record FamilyRankingResponse(string FamilyId, List<FamilyMemberRankDto> Rankings);
public record FamilyMemberRankDto(string UserId, string FullName, int Points, int Rank);
public record FamilyMembersResponse(string FamilyId, List<AuthUserResponseDto> Members);
public record FamilyChallengesResponse(string FamilyId, List<ChallengeProgressDto> Challenges);
public record FamilyRewardsResponse(string FamilyId, int TotalFamilyPoints, List<string> UnlockedBadges);
public record FamilyHeatmapResponse(string FamilyId, List<int> CombinedHourlyHeatmap);
