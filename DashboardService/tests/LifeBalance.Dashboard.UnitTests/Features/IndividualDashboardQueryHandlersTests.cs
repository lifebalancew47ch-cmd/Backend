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

    [Fact]
    public async Task Handle_GetIndividualDashboardQuery_ReturnsSuccessfulResult()
    {
        // Arrange
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUserResponseDto(userId, "test@lifebalance.io", "John", "Doe", new List<string> { "User" }, "fam_1", "comp_1"));

        var query = new GetIndividualDashboardQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.UserProfile.UserId.Should().Be(userId);
        result.Value.UserProfile.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Handle_GetIndividualSummaryQuery_ReturnsSummary()
    {
        // Arrange
        var userId = "usr_test_123";
        _authClient.GetUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUserResponseDto(userId, "test@lifebalance.io", "Jane", "Doe", new List<string> { "User" }, "fam_1", "comp_1"));

        var query = new GetIndividualSummaryQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Jane Doe");
    }
}
