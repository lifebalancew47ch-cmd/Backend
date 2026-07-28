using LifeBalance.Dashboard.Application.Common.Interfaces;

namespace LifeBalance.Dashboard.Application.Features.IndividualDashboard;

public record IndividualDashboardResponse(
    AuthUserResponseDto UserProfile,
    MedicalDataResponseDto? Biometrics,
    SedentaryActivityResponseDto? Activity,
    UserRewardsResponseDto? Rewards,
    List<NotificationItemDto> Notifications,
    List<RecommendationDto> Recommendations
);

public record IndividualSummaryResponse(string UserId, string FullName, int DailySteps, double ActiveMinutes, int Points, int StreakDays);
public record IndividualKpisResponse(string UserId, double Bmi, double HeartRate, int DailySteps, double CaloriesBurned);
public record IndividualStatisticsResponse(string UserId, double ActiveHoursThisWeek, double SedentaryHoursThisWeek, double AverageHeartRate);
public record IndividualHeatmapResponse(string UserId, List<int> HourlyHeatmap);
public record IndividualGoalsResponse(string UserId, List<GoalProgressDto> Goals);
public record IndividualProgressResponse(string UserId, double WeeklyGoalCompletionPercentage, int DaysActive);
public record IndividualActivityResponse(string UserId, SedentaryActivityResponseDto Activity);
public record IndividualRecommendationsResponse(string UserId, List<RecommendationDto> Recommendations);
public record IndividualRewardsResponse(string UserId, UserRewardsResponseDto Rewards);
public record IndividualNotificationsResponse(string UserId, List<NotificationItemDto> Notifications);
public record IndividualBiometricsResponse(string UserId, MedicalDataResponseDto Biometrics);
