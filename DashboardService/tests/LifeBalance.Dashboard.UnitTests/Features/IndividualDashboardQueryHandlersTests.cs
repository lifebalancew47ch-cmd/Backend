using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.IndividualDashboard;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class IndividualDashboardQueryHandlersTests
{
    private readonly IAuthServiceClient _authClient = Substitute.For<IAuthServiceClient>();
    private readonly IMedicalDataServiceClient _medicalClient = Substitute.For<IMedicalDataServiceClient>();
    private readonly ISedentaryEngineServiceClient _sedentaryClient = Substitute.For<ISedentaryEngineServiceClient>();
    private readonly IGamificationServiceClient _gamificationClient = Substitute.For<IGamificationServiceClient>();
    private readonly INotificationServiceClient _notificationClient = Substitute.For<INotificationServiceClient>();
    private readonly IMlPredictionServiceClient _mlClient = Substitute.For<IMlPredictionServiceClient>();

    private readonly IndividualDashboardQueryHandlers _handler;

    public IndividualDashboardQueryHandlersTests()
    {
        _handler = new IndividualDashboardQueryHandlers(
            _authClient,
            _medicalClient,
            _sedentaryClient,
            _gamificationClient,
            _notificationClient,
            _mlClient);
    }

    private static AuthUserResponseDto CreateProfile(string userId) =>
        new(userId, "test@lifebalance.io", "John", "Doe", new List<string> { "User" }, "fam_1", "comp_1");

    // ── GET /api/v1/dashboard/individual ──

    [Fact]
    public async Task Handle_GetIndividualDashboardQuery_ReturnsSuccessfulResult()
    {
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(CreateProfile(userId));

        var result = await _handler.Handle(new GetIndividualDashboardQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.UserProfile.UserId.Should().Be(userId);
        result.Value.UserProfile.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Handle_GetIndividualDashboardQuery_AllDownstreamNull_UsesFallbackProfile()
    {
        var userId = "usr_fallback";

        var result = await _handler.Handle(new GetIndividualDashboardQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserProfile.UserId.Should().Be(userId);
        result.Value.UserProfile.Email.Should().Be("user@lifebalance.io");
    }

    [Fact]
    public async Task Handle_GetIndividualDashboardQuery_NotificationsNull_ReturnsEmptyList()
    {
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(CreateProfile(userId));
        _notificationClient.GetUserNotificationsAsync(userId, 10, Arg.Any<CancellationToken>()).Returns((List<NotificationItemDto>?)null);

        var result = await _handler.Handle(new GetIndividualDashboardQuery(userId), CancellationToken.None);

        result.Value.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetIndividualDashboardQuery_AggregatesAllSixSources()
    {
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(CreateProfile(userId));

        await _handler.Handle(new GetIndividualDashboardQuery(userId), CancellationToken.None);

        await _authClient.Received(1).GetUserProfileAsync(userId, Arg.Any<CancellationToken>());
        await _medicalClient.Received(1).GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>());
        await _sedentaryClient.Received(1).GetUserActivityAsync(userId, Arg.Any<CancellationToken>());
        await _gamificationClient.Received(1).GetUserRewardsAsync(userId, Arg.Any<CancellationToken>());
        await _notificationClient.Received(1).GetUserNotificationsAsync(userId, 10, Arg.Any<CancellationToken>());
        await _mlClient.Received(1).GetRecommendationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetIndividualDashboardQuery_PreservesReturnedData()
    {
        var userId = "usr_test_123";
        var biometrics = new MedicalDataResponseDto(userId, 65, 110, 70, 68, 1.70, 23.5, DateTime.UtcNow);
        _medicalClient.GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>()).Returns(biometrics);
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(CreateProfile(userId));

        var result = await _handler.Handle(new GetIndividualDashboardQuery(userId), CancellationToken.None);

        result.Value.Biometrics.Should().Be(biometrics);
    }

    // ── GET /api/v1/dashboard/individual/summary ──

    [Fact]
    public async Task Handle_GetIndividualSummaryQuery_ReturnsSummary()
    {
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUserResponseDto(userId, "test@lifebalance.io", "Jane", "Doe", new List<string> { "User" }, "fam_1", "comp_1"));

        var result = await _handler.Handle(new GetIndividualSummaryQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task Handle_GetIndividualSummaryQuery_NullProfile_UsesDefaultName()
    {
        var result = await _handler.Handle(new GetIndividualSummaryQuery("usr_ghost"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("User");
    }

    [Fact]
    public async Task Handle_GetIndividualSummaryQuery_NullActivityAndRewards_DefaultsToZero()
    {
        var result = await _handler.Handle(new GetIndividualSummaryQuery("usr_ghost"), CancellationToken.None);

        result.Value.DailySteps.Should().Be(0);
        result.Value.ActiveMinutes.Should().Be(0);
        result.Value.Points.Should().Be(0);
        result.Value.StreakDays.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetIndividualSummaryQuery_PropagatesActivityAndRewards()
    {
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(CreateProfile(userId));
        _sedentaryClient.GetUserActivityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SedentaryActivityResponseDto(userId, 12000, 90, 4.0, 600, new List<int>()));
        _gamificationClient.GetUserRewardsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserRewardsResponseDto(userId, 2500, 12, 21, new List<string>()));

        var result = await _handler.Handle(new GetIndividualSummaryQuery(userId), CancellationToken.None);

        result.Value.DailySteps.Should().Be(12000);
        result.Value.ActiveMinutes.Should().Be(90);
        result.Value.Points.Should().Be(2500);
        result.Value.StreakDays.Should().Be(21);
    }

    [Fact]
    public async Task Handle_GetIndividualSummaryQuery_CallsAuthSedentaryAndGamification()
    {
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(CreateProfile(userId));

        await _handler.Handle(new GetIndividualSummaryQuery(userId), CancellationToken.None);

        await _authClient.Received(1).GetUserProfileAsync(userId, Arg.Any<CancellationToken>());
        await _sedentaryClient.Received(1).GetUserActivityAsync(userId, Arg.Any<CancellationToken>());
        await _gamificationClient.Received(1).GetUserRewardsAsync(userId, Arg.Any<CancellationToken>());
    }

    // ── GET /api/v1/dashboard/individual/kpis ──

    [Fact]
    public async Task Handle_GetIndividualKpisQuery_ReturnsKpisFromSources()
    {
        var userId = "usr_test_123";
        _medicalClient.GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MedicalDataResponseDto(userId, 60, 120, 80, 70, 1.75, 22.8, DateTime.UtcNow));
        _sedentaryClient.GetUserActivityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SedentaryActivityResponseDto(userId, 9500, 60, 5.0, 420, new List<int>()));

        var result = await _handler.Handle(new GetIndividualKpisQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Bmi.Should().Be(22.8);
        result.Value.HeartRate.Should().Be(60);
        result.Value.DailySteps.Should().Be(9500);
        result.Value.CaloriesBurned.Should().Be(420);
    }

    [Fact]
    public async Task Handle_GetIndividualKpisQuery_NullBiometrics_UsesDefaults()
    {
        var result = await _handler.Handle(new GetIndividualKpisQuery("usr_ghost"), CancellationToken.None);

        result.Value.Bmi.Should().Be(22.5);
        result.Value.HeartRate.Should().Be(72);
    }

    [Fact]
    public async Task Handle_GetIndividualKpisQuery_NullActivity_UsesDefaults()
    {
        var result = await _handler.Handle(new GetIndividualKpisQuery("usr_ghost"), CancellationToken.None);

        result.Value.DailySteps.Should().Be(8500);
        result.Value.CaloriesBurned.Should().Be(450);
    }

    [Fact]
    public async Task Handle_GetIndividualKpisQuery_CallsMedicalAndSedentary()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualKpisQuery(userId), CancellationToken.None);

        await _medicalClient.Received(1).GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>());
        await _sedentaryClient.Received(1).GetUserActivityAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetIndividualKpisQuery_ReturnsUserIdEcho()
    {
        var result = await _handler.Handle(new GetIndividualKpisQuery("usr_echo"), CancellationToken.None);

        result.Value.UserId.Should().Be("usr_echo");
    }

    // ── GET /api/v1/dashboard/individual/statistics ──

    [Fact]
    public async Task Handle_GetIndividualStatisticsQuery_ComputesActiveHoursFromMinutes()
    {
        var userId = "usr_test_123";
        _sedentaryClient.GetUserActivityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SedentaryActivityResponseDto(userId, 8000, 120, 4.0, 350, new List<int>()));

        var result = await _handler.Handle(new GetIndividualStatisticsQuery(userId), CancellationToken.None);

        result.Value.ActiveHoursThisWeek.Should().Be(2.0);
        result.Value.SedentaryHoursThisWeek.Should().Be(4.0);
    }

    [Fact]
    public async Task Handle_GetIndividualStatisticsQuery_NullActivity_UsesDefaults()
    {
        var result = await _handler.Handle(new GetIndividualStatisticsQuery("usr_ghost"), CancellationToken.None);

        result.Value.ActiveHoursThisWeek.Should().Be(3.0);
        result.Value.SedentaryHoursThisWeek.Should().Be(6.5);
    }

    [Fact]
    public async Task Handle_GetIndividualStatisticsQuery_NullBiometrics_UsesDefaultHeartRate()
    {
        var result = await _handler.Handle(new GetIndividualStatisticsQuery("usr_ghost"), CancellationToken.None);

        result.Value.AverageHeartRate.Should().Be(72);
    }

    [Fact]
    public async Task Handle_GetIndividualStatisticsQuery_HeartRateFromBiometrics()
    {
        var userId = "usr_test_123";
        _medicalClient.GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MedicalDataResponseDto(userId, 85, 130, 85, 70, 1.75, 22.8, DateTime.UtcNow));

        var result = await _handler.Handle(new GetIndividualStatisticsQuery(userId), CancellationToken.None);

        result.Value.AverageHeartRate.Should().Be(85);
    }

    [Fact]
    public async Task Handle_GetIndividualStatisticsQuery_CallsSedentaryAndMedical()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualStatisticsQuery(userId), CancellationToken.None);

        await _sedentaryClient.Received(1).GetUserActivityAsync(userId, Arg.Any<CancellationToken>());
        await _medicalClient.Received(1).GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>());
    }

    // ── GET /api/v1/dashboard/individual/heatmap ──

    [Fact]
    public async Task Handle_GetIndividualHeatmapQuery_ReturnsActivityHeatmap()
    {
        var userId = "usr_test_123";
        var heatmap = Enumerable.Range(1, 24).ToList();
        _sedentaryClient.GetUserActivityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SedentaryActivityResponseDto(userId, 8000, 45, 6.0, 350, heatmap));

        var result = await _handler.Handle(new GetIndividualHeatmapQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HourlyHeatmap.Should().BeEquivalentTo(heatmap);
    }

    [Fact]
    public async Task Handle_GetIndividualHeatmapQuery_NullActivity_ReturnsAllZeros()
    {
        var result = await _handler.Handle(new GetIndividualHeatmapQuery("usr_ghost"), CancellationToken.None);

        result.Value.HourlyHeatmap.Should().HaveCount(24);
        result.Value.HourlyHeatmap.Should().OnlyContain(v => v == 0);
    }

    [Fact]
    public async Task Handle_GetIndividualHeatmapQuery_HeatmapHas24Entries()
    {
        var userId = "usr_test_123";
        _sedentaryClient.GetUserActivityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SedentaryActivityResponseDto(userId, 8000, 45, 6.0, 350, Enumerable.Repeat(3, 24).ToList()));

        var result = await _handler.Handle(new GetIndividualHeatmapQuery(userId), CancellationToken.None);

        result.Value.HourlyHeatmap.Should().HaveCount(24);
    }

    [Fact]
    public async Task Handle_GetIndividualHeatmapQuery_CallsSedentaryClient()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualHeatmapQuery(userId), CancellationToken.None);

        await _sedentaryClient.Received(1).GetUserActivityAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetIndividualHeatmapQuery_ReturnsUserIdEcho()
    {
        var result = await _handler.Handle(new GetIndividualHeatmapQuery("usr_heat"), CancellationToken.None);

        result.Value.UserId.Should().Be("usr_heat");
    }

    // ── GET /api/v1/dashboard/individual/goals ──

    [Fact]
    public async Task Handle_GetIndividualGoalsQuery_ReturnsChallenges()
    {
        var userId = "usr_test_123";
        var challenges = new List<ChallengeProgressDto> { new("c1", "Walk 10k", 50.0, false) };
        _gamificationClient.GetFamilyChallengesAsync("user_default_family", Arg.Any<CancellationToken>()).Returns(challenges);

        var result = await _handler.Handle(new GetIndividualGoalsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Goals.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_GetIndividualGoalsQuery_NullChallenges_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetIndividualGoalsQuery("usr_ghost"), CancellationToken.None);

        result.Value.Goals.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetIndividualGoalsQuery_UsesDefaultFamilyId()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualGoalsQuery(userId), CancellationToken.None);

        await _gamificationClient.Received(1).GetFamilyChallengesAsync("user_default_family", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetIndividualGoalsQuery_PropagatesChallengeData()
    {
        var userId = "usr_test_123";
        _gamificationClient.GetFamilyChallengesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChallengeProgressDto> { new("c9", "Hydration", 100.0, true) });

        var result = await _handler.Handle(new GetIndividualGoalsQuery(userId), CancellationToken.None);

        result.Value.Goals.Single().Completed.Should().BeTrue();
        result.Value.Goals.Single().Title.Should().Be("Hydration");
    }

    [Fact]
    public async Task Handle_GetIndividualGoalsQuery_ReturnsUserIdEcho()
    {
        var result = await _handler.Handle(new GetIndividualGoalsQuery("usr_goal"), CancellationToken.None);

        result.Value.UserId.Should().Be("usr_goal");
    }

    // ── GET /api/v1/dashboard/individual/progress ──

    [Fact]
    public async Task Handle_GetIndividualProgressQuery_ReturnsFixedValues()
    {
        var result = await _handler.Handle(new GetIndividualProgressQuery("usr_test_123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WeeklyGoalCompletionPercentage.Should().Be(85.0);
        result.Value.DaysActive.Should().Be(5);
    }

    [Fact]
    public async Task Handle_GetIndividualProgressQuery_DoesNotCallDownstream()
    {
        await _handler.Handle(new GetIndividualProgressQuery("usr_test_123"), CancellationToken.None);

        await _authClient.DidNotReceiveWithAnyArgs().GetUserProfileAsync(default, default);
        await _sedentaryClient.DidNotReceiveWithAnyArgs().GetUserActivityAsync(default, default);
    }

    [Fact]
    public async Task Handle_GetIndividualProgressQuery_StableAcrossCalls()
    {
        var first = await _handler.Handle(new GetIndividualProgressQuery("u1"), CancellationToken.None);
        var second = await _handler.Handle(new GetIndividualProgressQuery("u1"), CancellationToken.None);

        first.Value.Should().BeEquivalentTo(second.Value);
    }

    [Fact]
    public async Task Handle_GetIndividualProgressQuery_SucceedsForAnyUser()
    {
        var result = await _handler.Handle(new GetIndividualProgressQuery("usr_unknown"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetIndividualProgressQuery_ReturnsUserIdEcho()
    {
        var result = await _handler.Handle(new GetIndividualProgressQuery("usr_prog"), CancellationToken.None);

        result.Value.UserId.Should().Be("usr_prog");
    }

    // ── GET /api/v1/dashboard/individual/activity ──

    [Fact]
    public async Task Handle_GetIndividualActivityQuery_ReturnsActivity()
    {
        var userId = "usr_test_123";
        var activity = new SedentaryActivityResponseDto(userId, 10500, 75, 5.5, 500, new List<int>());
        _sedentaryClient.GetUserActivityAsync(userId, Arg.Any<CancellationToken>()).Returns(activity);

        var result = await _handler.Handle(new GetIndividualActivityQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Activity.Should().Be(activity);
    }

    [Fact]
    public async Task Handle_GetIndividualActivityQuery_NullActivity_UsesFallbackDefaults()
    {
        var result = await _handler.Handle(new GetIndividualActivityQuery("usr_ghost"), CancellationToken.None);

        result.Value.Activity.DailySteps.Should().Be(8000);
        result.Value.Activity.ActiveMinutes.Should().Be(45);
        result.Value.Activity.SedentaryHours.Should().Be(6.0);
        result.Value.Activity.CaloriesBurned.Should().Be(350);
    }

    [Fact]
    public async Task Handle_GetIndividualActivityQuery_FallbackHas24HeatmapEntries()
    {
        var result = await _handler.Handle(new GetIndividualActivityQuery("usr_ghost"), CancellationToken.None);

        result.Value.Activity.HourlyHeatmap.Should().HaveCount(24);
    }

    [Fact]
    public async Task Handle_GetIndividualActivityQuery_PropagatesStepCount()
    {
        var userId = "usr_test_123";
        _sedentaryClient.GetUserActivityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SedentaryActivityResponseDto(userId, 15000, 100, 3.0, 700, new List<int>()));

        var result = await _handler.Handle(new GetIndividualActivityQuery(userId), CancellationToken.None);

        result.Value.Activity.DailySteps.Should().Be(15000);
    }

    [Fact]
    public async Task Handle_GetIndividualActivityQuery_CallsSedentaryClient()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualActivityQuery(userId), CancellationToken.None);

        await _sedentaryClient.Received(1).GetUserActivityAsync(userId, Arg.Any<CancellationToken>());
    }

    // ── GET /api/v1/dashboard/individual/recommendations ──

    [Fact]
    public async Task Handle_GetIndividualRecommendationsQuery_ReturnsRecommendations()
    {
        var userId = "usr_test_123";
        var recs = new List<RecommendationDto> { new("r1", "fitness", "Move", "Take a walk", 0.9) };
        _mlClient.GetRecommendationsAsync(userId, Arg.Any<CancellationToken>()).Returns(recs);

        var result = await _handler.Handle(new GetIndividualRecommendationsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_GetIndividualRecommendationsQuery_NullRecommendations_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetIndividualRecommendationsQuery("usr_ghost"), CancellationToken.None);

        result.Value.Recommendations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetIndividualRecommendationsQuery_PropagatesPriority()
    {
        var userId = "usr_test_123";
        _mlClient.GetRecommendationsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<RecommendationDto> { new("r2", "nutrition", "Eat", "More fiber", 0.7) });

        var result = await _handler.Handle(new GetIndividualRecommendationsQuery(userId), CancellationToken.None);

        result.Value.Recommendations.Single().PriorityScore.Should().Be(0.7);
    }

    [Fact]
    public async Task Handle_GetIndividualRecommendationsQuery_CallsMlClient()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualRecommendationsQuery(userId), CancellationToken.None);

        await _mlClient.Received(1).GetRecommendationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetIndividualRecommendationsQuery_ReturnsUserIdEcho()
    {
        var result = await _handler.Handle(new GetIndividualRecommendationsQuery("usr_rec"), CancellationToken.None);

        result.Value.UserId.Should().Be("usr_rec");
    }

    // ── GET /api/v1/dashboard/individual/rewards ──

    [Fact]
    public async Task Handle_GetIndividualRewardsQuery_ReturnsRewards()
    {
        var userId = "usr_test_123";
        var rewards = new UserRewardsResponseDto(userId, 3000, 15, 30, new List<string> { "Marathon" });
        _gamificationClient.GetUserRewardsAsync(userId, Arg.Any<CancellationToken>()).Returns(rewards);

        var result = await _handler.Handle(new GetIndividualRewardsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rewards.Should().Be(rewards);
    }

    [Fact]
    public async Task Handle_GetIndividualRewardsQuery_NullRewards_UsesFallback()
    {
        var result = await _handler.Handle(new GetIndividualRewardsQuery("usr_ghost"), CancellationToken.None);

        result.Value.Rewards.Points.Should().Be(1500);
        result.Value.Rewards.CurrentStreakDays.Should().Be(7);
    }

    [Fact]
    public async Task Handle_GetIndividualRewardsQuery_FallbackHasBadges()
    {
        var result = await _handler.Handle(new GetIndividualRewardsQuery("usr_ghost"), CancellationToken.None);

        result.Value.Rewards.RecentRewards.Should().Contain("First Step");
        result.Value.Rewards.RecentRewards.Should().Contain("7 Day Streak");
    }

    [Fact]
    public async Task Handle_GetIndividualRewardsQuery_PropagatesPoints()
    {
        var userId = "usr_test_123";
        _gamificationClient.GetUserRewardsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserRewardsResponseDto(userId, 9999, 20, 40, new List<string>()));

        var result = await _handler.Handle(new GetIndividualRewardsQuery(userId), CancellationToken.None);

        result.Value.Rewards.Points.Should().Be(9999);
    }

    [Fact]
    public async Task Handle_GetIndividualRewardsQuery_CallsGamificationClient()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualRewardsQuery(userId), CancellationToken.None);

        await _gamificationClient.Received(1).GetUserRewardsAsync(userId, Arg.Any<CancellationToken>());
    }

    // ── GET /api/v1/dashboard/individual/notifications ──

    [Fact]
    public async Task Handle_GetIndividualNotificationsQuery_ReturnsNotifications()
    {
        var userId = "usr_test_123";
        var notes = new List<NotificationItemDto> { new("n1", "Reminder", "Stand up", "info", DateTime.UtcNow, false) };
        _notificationClient.GetUserNotificationsAsync(userId, 10, Arg.Any<CancellationToken>()).Returns(notes);

        var result = await _handler.Handle(new GetIndividualNotificationsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Notifications.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_GetIndividualNotificationsQuery_NullNotifications_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetIndividualNotificationsQuery("usr_ghost"), CancellationToken.None);

        result.Value.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetIndividualNotificationsQuery_RequestsTopTen()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualNotificationsQuery(userId), CancellationToken.None);

        await _notificationClient.Received(1).GetUserNotificationsAsync(userId, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetIndividualNotificationsQuery_PropagatesTitles()
    {
        var userId = "usr_test_123";
        _notificationClient.GetUserNotificationsAsync(userId, 10, Arg.Any<CancellationToken>())
            .Returns(new List<NotificationItemDto> { new("n2", "License", "Expiring soon", "warning", DateTime.UtcNow, false) });

        var result = await _handler.Handle(new GetIndividualNotificationsQuery(userId), CancellationToken.None);

        result.Value.Notifications.Single().Title.Should().Be("License");
    }

    [Fact]
    public async Task Handle_GetIndividualNotificationsQuery_ReturnsUserIdEcho()
    {
        var result = await _handler.Handle(new GetIndividualNotificationsQuery("usr_notif"), CancellationToken.None);

        result.Value.UserId.Should().Be("usr_notif");
    }

    // ── GET /api/v1/dashboard/individual/biometrics ──

    [Fact]
    public async Task Handle_GetIndividualBiometricsQuery_ReturnsBiometrics()
    {
        var userId = "usr_test_123";
        var bio = new MedicalDataResponseDto(userId, 70, 118, 78, 72, 1.80, 22.2, DateTime.UtcNow);
        _medicalClient.GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>()).Returns(bio);

        var result = await _handler.Handle(new GetIndividualBiometricsQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Biometrics.Should().Be(bio);
    }

    [Fact]
    public async Task Handle_GetIndividualBiometricsQuery_NullBiometrics_UsesFallbackDefaults()
    {
        var result = await _handler.Handle(new GetIndividualBiometricsQuery("usr_ghost"), CancellationToken.None);

        result.Value.Biometrics.HeartRate.Should().Be(72);
        result.Value.Biometrics.Bmi.Should().Be(22.8);
    }

    [Fact]
    public async Task Handle_GetIndividualBiometricsQuery_PropagatesBloodPressure()
    {
        var userId = "usr_test_123";
        _medicalClient.GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MedicalDataResponseDto(userId, 95, 150, 95, 80, 1.75, 26.0, DateTime.UtcNow));

        var result = await _handler.Handle(new GetIndividualBiometricsQuery(userId), CancellationToken.None);

        result.Value.Biometrics.SystolicBp.Should().Be(150);
        result.Value.Biometrics.DiastolicBp.Should().Be(95);
    }

    [Fact]
    public async Task Handle_GetIndividualBiometricsQuery_CallsMedicalClient()
    {
        var userId = "usr_test_123";
        await _handler.Handle(new GetIndividualBiometricsQuery(userId), CancellationToken.None);

        await _medicalClient.Received(1).GetUserBiometricsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetIndividualBiometricsQuery_ReturnsUserIdEcho()
    {
        var result = await _handler.Handle(new GetIndividualBiometricsQuery("usr_bio"), CancellationToken.None);

        result.Value.UserId.Should().Be("usr_bio");
    }
}
