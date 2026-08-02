using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Exceptions;
using LifeBalance.Dashboard.Application.Features.FamilyDashboard;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class FamilyDashboardQueryHandlersTests
{
    private readonly IAuthServiceClient _authClient = Substitute.For<IAuthServiceClient>();
    private readonly IMedicalDataServiceClient _medicalClient = Substitute.For<IMedicalDataServiceClient>();
    private readonly IGamificationServiceClient _gamificationClient = Substitute.For<IGamificationServiceClient>();

    private readonly FamilyDashboardQueryHandlers _handler;

    public FamilyDashboardQueryHandlersTests()
    {
        _handler = new FamilyDashboardQueryHandlers(_authClient, _medicalClient, _gamificationClient);
    }

    private static List<AuthUserResponseDto> CreateMembers(string familyId) =>
        new()
        {
            new AuthUserResponseDto("u1", "fam1@lifebalance.io", "Alice", "Smith", new List<string> { "User" }, familyId, "c1"),
            new AuthUserResponseDto("u2", "fam2@lifebalance.io", "Bob", "Smith", new List<string> { "User" }, familyId, "c1")
        };

    private void StubFamilySources(string familyId)
    {
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));
        _medicalClient.GetFamilyBiometricsAsync(familyId, Arg.Any<CancellationToken>()).Returns(new List<MedicalDataResponseDto>());
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>()).Returns(new List<ChallengeProgressDto>());
    }

    // ── GET /api/v1/dashboard/family ──

    [Fact]
    public async Task Handle_GetFamilyDashboardQuery_ReturnsSuccessfulResult()
    {
        var familyId = "fam_test_001";
        StubFamilySources(familyId);

        var result = await _handler.Handle(new GetFamilyDashboardQuery(familyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FamilyId.Should().Be(familyId);
        result.Value.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_GetFamilyDashboardQuery_MembersUnavailable_ThrowsUpstreamUnavailable()
    {
        var familyId = "fam_empty";
        _medicalClient.GetFamilyBiometricsAsync(familyId, Arg.Any<CancellationToken>()).Returns(new List<MedicalDataResponseDto>());
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>()).Returns(new List<ChallengeProgressDto>());

        await FluentActions.Awaiting(() => _handler.Handle(new GetFamilyDashboardQuery(familyId), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetFamilyDashboardQuery_BiometricsUnavailable_ThrowsUpstreamUnavailable()
    {
        var familyId = "fam_empty";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>()).Returns(new List<ChallengeProgressDto>());

        await FluentActions.Awaiting(() => _handler.Handle(new GetFamilyDashboardQuery(familyId), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetFamilyDashboardQuery_AggregatesThreeSources()
    {
        var familyId = "fam_test_001";
        StubFamilySources(familyId);

        await _handler.Handle(new GetFamilyDashboardQuery(familyId), CancellationToken.None);

        await _authClient.Received(1).GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>());
        await _medicalClient.Received(1).GetFamilyBiometricsAsync(familyId, Arg.Any<CancellationToken>());
        await _gamificationClient.Received(1).GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetFamilyDashboardQuery_PreservesBiometricsAndChallenges()
    {
        var familyId = "fam_test_001";
        var biometrics = new List<MedicalDataResponseDto> { new("u1", 70, 120, 80, 70, 1.75, 22.8, DateTime.UtcNow) };
        var challenges = new List<ChallengeProgressDto> { new("c1", "Family Walk", 60.0, false) };
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));
        _medicalClient.GetFamilyBiometricsAsync(familyId, Arg.Any<CancellationToken>()).Returns(biometrics);
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>()).Returns(challenges);

        var result = await _handler.Handle(new GetFamilyDashboardQuery(familyId), CancellationToken.None);

        result.Value.FamilyBiometrics.Should().BeEquivalentTo(biometrics);
        result.Value.Challenges.Should().BeEquivalentTo(challenges);
    }

    [Fact]
    public async Task Handle_GetFamilyDashboardQuery_ReturnsFamilyIdEcho()
    {
        var familyId = "fam_echo";
        StubFamilySources(familyId);

        var result = await _handler.Handle(new GetFamilyDashboardQuery(familyId), CancellationToken.None);

        result.Value.FamilyId.Should().Be("fam_echo");
    }

    // ── GET /api/v1/dashboard/family/statistics ──

    [Fact]
    public async Task Handle_GetFamilyStatisticsQuery_CountsMembers()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        var result = await _handler.Handle(new GetFamilyStatisticsQuery(familyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MemberCount.Should().Be(2);
        result.Value.TotalFamilySteps.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetFamilyStatisticsQuery_NullMembers_ThrowsUpstreamUnavailable()
    {
        await FluentActions.Awaiting(() => _handler.Handle(new GetFamilyStatisticsQuery("fam_empty"), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetFamilyStatisticsQuery_StructuralCountsAreZero()
    {
        var familyId = "fam_x";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        var result = await _handler.Handle(new GetFamilyStatisticsQuery(familyId), CancellationToken.None);

        result.Value.AverageActiveMinutes.Should().Be(0.0);
        result.Value.TotalFamilySteps.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetFamilyStatisticsQuery_CallsAuthClient()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        await _handler.Handle(new GetFamilyStatisticsQuery(familyId), CancellationToken.None);

        await _authClient.Received(1).GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetFamilyStatisticsQuery_ThreeMembersCounted()
    {
        var familyId = "fam_test_001";
        var members = new List<AuthUserResponseDto>
        {
            new("u1", "a@x.io", "A", "A", new List<string>(), familyId, "c1"),
            new("u2", "b@x.io", "B", "B", new List<string>(), familyId, "c1"),
            new("u3", "c@x.io", "C", "C", new List<string>(), familyId, "c1")
        };
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(members);

        var result = await _handler.Handle(new GetFamilyStatisticsQuery(familyId), CancellationToken.None);

        result.Value.MemberCount.Should().Be(3);
        result.Value.TotalFamilySteps.Should().Be(0);
    }

    // ── GET /api/v1/dashboard/family/goals ──

    [Fact]
    public async Task Handle_GetFamilyGoalsQuery_ReturnsActiveChallenges()
    {
        var familyId = "fam_test_001";
        var challenges = new List<ChallengeProgressDto> { new("c1", "Daily Steps", 80.0, false) };
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>()).Returns(challenges);

        var result = await _handler.Handle(new GetFamilyGoalsQuery(familyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveGoals.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_GetFamilyGoalsQuery_NullChallenges_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetFamilyGoalsQuery("fam_empty"), CancellationToken.None);

        result.Value.ActiveGoals.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetFamilyGoalsQuery_PropagatesProgress()
    {
        var familyId = "fam_test_001";
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>())
            .Returns(new List<ChallengeProgressDto> { new("c2", "Hydration", 100.0, true) });

        var result = await _handler.Handle(new GetFamilyGoalsQuery(familyId), CancellationToken.None);

        result.Value.ActiveGoals.Single().Completed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetFamilyGoalsQuery_CallsGamificationClient()
    {
        var familyId = "fam_test_001";
        await _handler.Handle(new GetFamilyGoalsQuery(familyId), CancellationToken.None);

        await _gamificationClient.Received(1).GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetFamilyGoalsQuery_ReturnsFamilyIdEcho()
    {
        var result = await _handler.Handle(new GetFamilyGoalsQuery("fam_goal"), CancellationToken.None);

        result.Value.FamilyId.Should().Be("fam_goal");
    }

    // ── GET /api/v1/dashboard/family/ranking ──

    [Fact]
    public async Task Handle_GetFamilyRankingQuery_RanksMembersInOrder()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        var result = await _handler.Handle(new GetFamilyRankingQuery(familyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rankings.Should().HaveCount(2);
        result.Value.Rankings[0].Rank.Should().Be(1);
        result.Value.Rankings[1].Rank.Should().Be(2);
    }

    [Fact]
    public async Task Handle_GetFamilyRankingQuery_PointsAreZeroWithoutUpstreamScores()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        var result = await _handler.Handle(new GetFamilyRankingQuery(familyId), CancellationToken.None);

        result.Value.Rankings.Should().OnlyContain(r => r.Points == 0);
    }

    [Fact]
    public async Task Handle_GetFamilyRankingQuery_NullMembers_ThrowsUpstreamUnavailable()
    {
        await FluentActions.Awaiting(() => _handler.Handle(new GetFamilyRankingQuery("fam_empty"), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetFamilyRankingQuery_FullNameComposed()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        var result = await _handler.Handle(new GetFamilyRankingQuery(familyId), CancellationToken.None);

        result.Value.Rankings[0].FullName.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task Handle_GetFamilyRankingQuery_CallsAuthClient()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        await _handler.Handle(new GetFamilyRankingQuery(familyId), CancellationToken.None);

        await _authClient.Received(1).GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>());
    }

    // ── GET /api/v1/dashboard/family/members ──

    [Fact]
    public async Task Handle_GetFamilyMembersQuery_ReturnsMembers()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        var result = await _handler.Handle(new GetFamilyMembersQuery(familyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_GetFamilyMembersQuery_NullMembers_ThrowsUpstreamUnavailable()
    {
        await FluentActions.Awaiting(() => _handler.Handle(new GetFamilyMembersQuery("fam_empty"), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetFamilyMembersQuery_PreservesMemberData()
    {
        var familyId = "fam_test_001";
        var members = CreateMembers(familyId);
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(members);

        var result = await _handler.Handle(new GetFamilyMembersQuery(familyId), CancellationToken.None);

        result.Value.Members.Should().BeEquivalentTo(members);
    }

    [Fact]
    public async Task Handle_GetFamilyMembersQuery_CallsAuthClient()
    {
        var familyId = "fam_test_001";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        await _handler.Handle(new GetFamilyMembersQuery(familyId), CancellationToken.None);

        await _authClient.Received(1).GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetFamilyMembersQuery_ReturnsFamilyIdEcho()
    {
        var familyId = "fam_mem";
        _authClient.GetFamilyMembersProfileAsync(familyId, Arg.Any<CancellationToken>()).Returns(CreateMembers(familyId));

        var result = await _handler.Handle(new GetFamilyMembersQuery(familyId), CancellationToken.None);

        result.Value.FamilyId.Should().Be("fam_mem");
    }

    // ── GET /api/v1/dashboard/family/challenges ──

    [Fact]
    public async Task Handle_GetFamilyChallengesQuery_ReturnsChallenges()
    {
        var familyId = "fam_test_001";
        var challenges = new List<ChallengeProgressDto> { new("c1", "Step Challenge", 50.0, false) };
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>()).Returns(challenges);

        var result = await _handler.Handle(new GetFamilyChallengesQuery(familyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Challenges.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_GetFamilyChallengesQuery_NullChallenges_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetFamilyChallengesQuery("fam_empty"), CancellationToken.None);

        result.Value.Challenges.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetFamilyChallengesQuery_PropagatesTitles()
    {
        var familyId = "fam_test_001";
        _gamificationClient.GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>())
            .Returns(new List<ChallengeProgressDto> { new("c7", "Water Intake", 30.0, false) });

        var result = await _handler.Handle(new GetFamilyChallengesQuery(familyId), CancellationToken.None);

        result.Value.Challenges.Single().Title.Should().Be("Water Intake");
    }

    [Fact]
    public async Task Handle_GetFamilyChallengesQuery_CallsGamificationClient()
    {
        var familyId = "fam_test_001";
        await _handler.Handle(new GetFamilyChallengesQuery(familyId), CancellationToken.None);

        await _gamificationClient.Received(1).GetFamilyChallengesAsync(familyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetFamilyChallengesQuery_ReturnsFamilyIdEcho()
    {
        var result = await _handler.Handle(new GetFamilyChallengesQuery("fam_chal"), CancellationToken.None);

        result.Value.FamilyId.Should().Be("fam_chal");
    }

    // ── GET /api/v1/dashboard/family/rewards ──

    [Fact]
    public async Task Handle_GetFamilyRewardsQuery_ThrowsWhenNoRewardsSource()
    {
        await FluentActions.Awaiting(() => _handler.Handle(new GetFamilyRewardsQuery("fam_test_001"), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetFamilyRewardsQuery_DoesNotCallDownstream()
    {
        await FluentActions.Awaiting(() => _handler.Handle(new GetFamilyRewardsQuery("fam_test_001"), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();

        await _authClient.DidNotReceiveWithAnyArgs().GetFamilyMembersProfileAsync(default, default);
        await _gamificationClient.DidNotReceiveWithAnyArgs().GetFamilyChallengesAsync(default, default);
    }

    // ── GET /api/v1/dashboard/family/heatmap ──

    [Fact]
    public async Task Handle_GetFamilyHeatmapQuery_Returns24EntryHeatmap()
    {
        var result = await _handler.Handle(new GetFamilyHeatmapQuery("fam_test_001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CombinedHourlyHeatmap.Should().HaveCount(24);
    }

    [Fact]
    public async Task Handle_GetFamilyHeatmapQuery_AllEntriesAreStructuralZeros()
    {
        var result = await _handler.Handle(new GetFamilyHeatmapQuery("fam_test_001"), CancellationToken.None);

        result.Value.CombinedHourlyHeatmap.Should().OnlyContain(v => v == 0);
    }

    [Fact]
    public async Task Handle_GetFamilyHeatmapQuery_DoesNotCallDownstream()
    {
        await _handler.Handle(new GetFamilyHeatmapQuery("fam_test_001"), CancellationToken.None);

        await _authClient.DidNotReceiveWithAnyArgs().GetFamilyMembersProfileAsync(default, default);
        await _medicalClient.DidNotReceiveWithAnyArgs().GetFamilyBiometricsAsync(default, default);
    }

    [Fact]
    public async Task Handle_GetFamilyHeatmapQuery_StableAcrossCalls()
    {
        var first = await _handler.Handle(new GetFamilyHeatmapQuery("fam_1"), CancellationToken.None);
        var second = await _handler.Handle(new GetFamilyHeatmapQuery("fam_1"), CancellationToken.None);

        first.Value.Should().BeEquivalentTo(second.Value);
    }

    [Fact]
    public async Task Handle_GetFamilyHeatmapQuery_ReturnsFamilyIdEcho()
    {
        var result = await _handler.Handle(new GetFamilyHeatmapQuery("fam_heat"), CancellationToken.None);

        result.Value.FamilyId.Should().Be("fam_heat");
    }
}
