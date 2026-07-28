using MediatR;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Shared.Results;

namespace LifeBalance.Dashboard.Application.Features.IndividualDashboard;

public record GetIndividualDashboardQuery(string UserId) : IRequest<Result<IndividualDashboardResponse>>;
public record GetIndividualSummaryQuery(string UserId) : IRequest<Result<IndividualSummaryResponse>>;
public record GetIndividualKpisQuery(string UserId) : IRequest<Result<IndividualKpisResponse>>;
public record GetIndividualStatisticsQuery(string UserId) : IRequest<Result<IndividualStatisticsResponse>>;
public record GetIndividualHeatmapQuery(string UserId) : IRequest<Result<IndividualHeatmapResponse>>;
public record GetIndividualGoalsQuery(string UserId) : IRequest<Result<IndividualGoalsResponse>>;
public record GetIndividualProgressQuery(string UserId) : IRequest<Result<IndividualProgressResponse>>;
public record GetIndividualActivityQuery(string UserId) : IRequest<Result<IndividualActivityResponse>>;
public record GetIndividualRecommendationsQuery(string UserId) : IRequest<Result<IndividualRecommendationsResponse>>;
public record GetIndividualRewardsQuery(string UserId) : IRequest<Result<IndividualRewardsResponse>>;
public record GetIndividualNotificationsQuery(string UserId) : IRequest<Result<IndividualNotificationsResponse>>;
public record GetIndividualBiometricsQuery(string UserId) : IRequest<Result<IndividualBiometricsResponse>>;

public class IndividualDashboardQueryHandlers :
    IRequestHandler<GetIndividualDashboardQuery, Result<IndividualDashboardResponse>>,
    IRequestHandler<GetIndividualSummaryQuery, Result<IndividualSummaryResponse>>,
    IRequestHandler<GetIndividualKpisQuery, Result<IndividualKpisResponse>>,
    IRequestHandler<GetIndividualStatisticsQuery, Result<IndividualStatisticsResponse>>,
    IRequestHandler<GetIndividualHeatmapQuery, Result<IndividualHeatmapResponse>>,
    IRequestHandler<GetIndividualGoalsQuery, Result<IndividualGoalsResponse>>,
    IRequestHandler<GetIndividualProgressQuery, Result<IndividualProgressResponse>>,
    IRequestHandler<GetIndividualActivityQuery, Result<IndividualActivityResponse>>,
    IRequestHandler<GetIndividualRecommendationsQuery, Result<IndividualRecommendationsResponse>>,
    IRequestHandler<GetIndividualRewardsQuery, Result<IndividualRewardsResponse>>,
    IRequestHandler<GetIndividualNotificationsQuery, Result<IndividualNotificationsResponse>>,
    IRequestHandler<GetIndividualBiometricsQuery, Result<IndividualBiometricsResponse>>
{
    private readonly IAuthServiceClient _authClient;
    private readonly IMedicalDataServiceClient _medicalClient;
    private readonly ISedentaryEngineServiceClient _sedentaryClient;
    private readonly IGamificationServiceClient _gamificationClient;
    private readonly INotificationServiceClient _notificationClient;
    private readonly IMlPredictionServiceClient _mlClient;

    public IndividualDashboardQueryHandlers(
        IAuthServiceClient authClient,
        IMedicalDataServiceClient medicalClient,
        ISedentaryEngineServiceClient sedentaryClient,
        IGamificationServiceClient gamificationClient,
        INotificationServiceClient notificationClient,
        IMlPredictionServiceClient mlClient)
    {
        _authClient = authClient;
        _medicalClient = medicalClient;
        _sedentaryClient = sedentaryClient;
        _gamificationClient = gamificationClient;
        _notificationClient = notificationClient;
        _mlClient = mlClient;
    }

    public async Task<Result<IndividualDashboardResponse>> Handle(GetIndividualDashboardQuery request, CancellationToken cancellationToken)
    {
        var userTask = _authClient.GetUserProfileAsync(request.UserId, cancellationToken);
        var biometricsTask = _medicalClient.GetUserBiometricsAsync(request.UserId, cancellationToken);
        var activityTask = _sedentaryClient.GetUserActivityAsync(request.UserId, cancellationToken);
        var rewardsTask = _gamificationClient.GetUserRewardsAsync(request.UserId, cancellationToken);
        var notificationsTask = _notificationClient.GetUserNotificationsAsync(request.UserId, 10, cancellationToken);
        var recommendationsTask = _mlClient.GetRecommendationsAsync(request.UserId, cancellationToken);

        await Task.WhenAll(userTask, biometricsTask, activityTask, rewardsTask, notificationsTask, recommendationsTask);

        var profile = await userTask ?? new AuthUserResponseDto(request.UserId, "user@lifebalance.io", "User", "LifeBalance", new List<string> { "User" }, "", "");
        var biometrics = await biometricsTask;
        var activity = await activityTask;
        var rewards = await rewardsTask;
        var notifications = await notificationsTask ?? new List<NotificationItemDto>();
        var recommendations = await recommendationsTask ?? new List<RecommendationDto>();

        return Result.Success(new IndividualDashboardResponse(
            profile,
            biometrics,
            activity,
            rewards,
            notifications,
            recommendations
        ));
    }

    public async Task<Result<IndividualSummaryResponse>> Handle(GetIndividualSummaryQuery request, CancellationToken cancellationToken)
    {
        var profile = await _authClient.GetUserProfileAsync(request.UserId, cancellationToken);
        var activity = await _sedentaryClient.GetUserActivityAsync(request.UserId, cancellationToken);
        var rewards = await _gamificationClient.GetUserRewardsAsync(request.UserId, cancellationToken);

        return Result.Success(new IndividualSummaryResponse(
            request.UserId,
            profile != null ? $"{profile.FirstName} {profile.LastName}" : "User",
            activity?.DailySteps ?? 0,
            activity?.ActiveMinutes ?? 0,
            rewards?.Points ?? 0,
            rewards?.CurrentStreakDays ?? 0
        ));
    }

    public async Task<Result<IndividualKpisResponse>> Handle(GetIndividualKpisQuery request, CancellationToken cancellationToken)
    {
        var biometrics = await _medicalClient.GetUserBiometricsAsync(request.UserId, cancellationToken);
        var activity = await _sedentaryClient.GetUserActivityAsync(request.UserId, cancellationToken);

        return Result.Success(new IndividualKpisResponse(
            request.UserId,
            biometrics?.Bmi ?? 22.5,
            biometrics?.HeartRate ?? 72,
            activity?.DailySteps ?? 8500,
            activity?.CaloriesBurned ?? 450
        ));
    }

    public async Task<Result<IndividualStatisticsResponse>> Handle(GetIndividualStatisticsQuery request, CancellationToken cancellationToken)
    {
        var activity = await _sedentaryClient.GetUserActivityAsync(request.UserId, cancellationToken);
        var biometrics = await _medicalClient.GetUserBiometricsAsync(request.UserId, cancellationToken);

        return Result.Success(new IndividualStatisticsResponse(
            request.UserId,
            (activity?.ActiveMinutes ?? 180) / 60.0,
            activity?.SedentaryHours ?? 6.5,
            biometrics?.HeartRate ?? 72
        ));
    }

    public async Task<Result<IndividualHeatmapResponse>> Handle(GetIndividualHeatmapQuery request, CancellationToken cancellationToken)
    {
        var activity = await _sedentaryClient.GetUserActivityAsync(request.UserId, cancellationToken);
        var heatmap = activity?.HourlyHeatmap ?? Enumerable.Repeat(0, 24).ToList();

        return Result.Success(new IndividualHeatmapResponse(request.UserId, heatmap));
    }

    public async Task<Result<IndividualGoalsResponse>> Handle(GetIndividualGoalsQuery request, CancellationToken cancellationToken)
    {
        var challenges = await _gamificationClient.GetFamilyChallengesAsync("user_default_family", cancellationToken);
        return Result.Success(new IndividualGoalsResponse(request.UserId, challenges));
    }

    public async Task<Result<IndividualProgressResponse>> Handle(GetIndividualProgressQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new IndividualProgressResponse(request.UserId, 85.0, 5));
    }

    public async Task<Result<IndividualActivityResponse>> Handle(GetIndividualActivityQuery request, CancellationToken cancellationToken)
    {
        var activity = await _sedentaryClient.GetUserActivityAsync(request.UserId, cancellationToken)
                       ?? new SedentaryActivityResponseDto(request.UserId, 8000, 45, 6.0, 350, Enumerable.Repeat(0, 24).ToList());

        return Result.Success(new IndividualActivityResponse(request.UserId, activity));
    }

    public async Task<Result<IndividualRecommendationsResponse>> Handle(GetIndividualRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var recs = await _mlClient.GetRecommendationsAsync(request.UserId, cancellationToken);
        return Result.Success(new IndividualRecommendationsResponse(request.UserId, recs ?? new List<RecommendationDto>()));
    }

    public async Task<Result<IndividualRewardsResponse>> Handle(GetIndividualRewardsQuery request, CancellationToken cancellationToken)
    {
        var rewards = await _gamificationClient.GetUserRewardsAsync(request.UserId, cancellationToken)
                      ?? new UserRewardsResponseDto(request.UserId, 1500, 5, 7, new List<string> { "First Step", "7 Day Streak" });

        return Result.Success(new IndividualRewardsResponse(request.UserId, rewards));
    }

    public async Task<Result<IndividualNotificationsResponse>> Handle(GetIndividualNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notes = await _notificationClient.GetUserNotificationsAsync(request.UserId, 10, cancellationToken);
        return Result.Success(new IndividualNotificationsResponse(request.UserId, notes ?? new List<NotificationItemDto>()));
    }

    public async Task<Result<IndividualBiometricsResponse>> Handle(GetIndividualBiometricsQuery request, CancellationToken cancellationToken)
    {
        var bio = await _medicalClient.GetUserBiometricsAsync(request.UserId, cancellationToken)
                  ?? new MedicalDataResponseDto(request.UserId, 72, 120, 80, 70, 1.75, 22.8, DateTime.UtcNow);

        return Result.Success(new IndividualBiometricsResponse(request.UserId, bio));
    }
}
