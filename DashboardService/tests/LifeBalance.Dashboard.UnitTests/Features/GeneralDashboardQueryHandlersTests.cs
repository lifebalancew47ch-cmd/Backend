using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.GeneralDashboard;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class GeneralDashboardQueryHandlersTests
{
    private readonly IReportingServiceClient _reportingClient = Substitute.For<IReportingServiceClient>();
    private readonly GeneralDashboardQueryHandlers _handler;

    public GeneralDashboardQueryHandlersTests()
    {
        _handler = new GeneralDashboardQueryHandlers(_reportingClient);
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_ReturnsSummary()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new GeneralSystemMetricsDto(5000, 1500, 99.9, "1.0.0"));

        var query = new GetGeneralSummaryQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveUsers.Should().Be(1500);
        result.Value.GlobalHealthScore.Should().Be(99.9);
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_ReportingDown_UsesFallbackDefaults()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns((GeneralSystemMetricsDto?)null);

        // Act
        var result = await _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveUsers.Should().Be(1250);
        result.Value.GlobalHealthScore.Should().Be(99.8);
        result.Value.SystemStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_MetricsValues_ArePropagated()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new GeneralSystemMetricsDto(9000, 3100, 97.5, "2.1.0"));

        // Act
        var result = await _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None);

        // Assert
        result.Value.ActiveUsers.Should().Be(3100);
        result.Value.GlobalHealthScore.Should().Be(97.5);
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_SystemStatus_AlwaysHealthy()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None);

        // Assert
        result.Value.SystemStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_ReportingClient_InvokedOnce()
    {
        // Act
        await _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None);

        // Assert
        await _reportingClient.Received(1).GetSystemMetricsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetGeneralIndicatorsQuery_ReturnsFixedPlatformValues()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralIndicatorsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AverageDailySteps.Should().Be(8200.0);
        result.Value.AverageSedentaryTime.Should().Be(5.8);
        result.Value.PlatformAdherenceRate.Should().Be(86.4);
    }

    [Fact]
    public async Task Handle_GetGeneralIndicatorsQuery_AlwaysSucceedsWithoutDownstream()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralIndicatorsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_GetGeneralIndicatorsQuery_DoesNotCallReportingClient()
    {
        // Act
        await _handler.Handle(new GetGeneralIndicatorsQuery(), CancellationToken.None);

        // Assert
        await _reportingClient.DidNotReceive().GetSystemMetricsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetGeneralIndicatorsQuery_ValuesAreStableAcrossCalls()
    {
        // Act
        var first = await _handler.Handle(new GetGeneralIndicatorsQuery(), CancellationToken.None);
        var second = await _handler.Handle(new GetGeneralIndicatorsQuery(), CancellationToken.None);

        // Assert
        first.Value.Should().BeEquivalentTo(second.Value);
    }

    [Fact]
    public async Task Handle_GetGeneralIndicatorsQuery_HonorsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _handler.Handle(new GetGeneralIndicatorsQuery(), cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetGeneralKpisQuery_ReturnsMetrics()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new GeneralSystemMetricsDto(12000, 4000, 99.0, "3.0.0"));

        // Act
        var result = await _handler.Handle(new GetGeneralKpisQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRegisteredUsers.Should().Be(12000);
    }

    [Fact]
    public async Task Handle_GetGeneralKpisQuery_ReportingDown_UsesFallbackUserCount()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns((GeneralSystemMetricsDto?)null);

        // Act
        var result = await _handler.Handle(new GetGeneralKpisQuery(), CancellationToken.None);

        // Assert
        result.Value.TotalRegisteredUsers.Should().Be(5000);
    }

    [Fact]
    public async Task Handle_GetGeneralKpisQuery_FamiliesAndCompanies_AreConstant()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralKpisQuery(), CancellationToken.None);

        // Assert
        result.Value.ActiveFamilies.Should().Be(450);
        result.Value.ActiveCompanies.Should().Be(35);
    }

    [Fact]
    public async Task Handle_GetGeneralKpisQuery_ReportingClient_InvokedOnce()
    {
        // Act
        await _handler.Handle(new GetGeneralKpisQuery(), CancellationToken.None);

        // Assert
        await _reportingClient.Received(1).GetSystemMetricsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetGeneralKpisQuery_MetricsUserCount_Propagated()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new GeneralSystemMetricsDto(777, 300, 88.0, "1.2.3"));

        // Act
        var result = await _handler.Handle(new GetGeneralKpisQuery(), CancellationToken.None);

        // Assert
        result.Value.TotalRegisteredUsers.Should().Be(777);
    }

    [Fact]
    public async Task Handle_GetGeneralSystemQuery_ReturnsOnlineStatus()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralSystemQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("Dashboard Service Aggregator");
        result.Value.Status.Should().Be("Online");
    }

    [Fact]
    public async Task Handle_GetGeneralSystemQuery_ServerTimeIsRecent()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralSystemQuery(), CancellationToken.None);

        // Assert
        result.Value.ServerTimeUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Handle_GetGeneralSystemQuery_ReportsProductionEnvironment()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralSystemQuery(), CancellationToken.None);

        // Assert
        result.Value.Environment.Should().Be("Production");
    }

    [Fact]
    public async Task Handle_GetGeneralSystemQuery_DoesNotCallReportingClient()
    {
        // Act
        await _handler.Handle(new GetGeneralSystemQuery(), CancellationToken.None);

        // Assert
        await _reportingClient.DidNotReceive().GetSystemMetricsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetGeneralSystemQuery_AlwaysSucceeds()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralSystemQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetGeneralHealthQuery_ReturnsHealthyOverall()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralHealthQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task Handle_GetGeneralHealthQuery_ReportsAllNineComponents()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralHealthQuery(), CancellationToken.None);

        // Assert
        result.Value.ComponentHealth.Should().HaveCount(9);
    }

    [Fact]
    public async Task Handle_GetGeneralHealthQuery_IncludesMongoDbComponent()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralHealthQuery(), CancellationToken.None);

        // Assert
        result.Value.ComponentHealth.Should().ContainKey("MongoDB");
        result.Value.ComponentHealth["MongoDB"].Should().Be("Healthy");
    }

    [Fact]
    public async Task Handle_GetGeneralHealthQuery_AllCoreServicesReported()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralHealthQuery(), CancellationToken.None);

        // Assert
        result.Value.ComponentHealth.Keys.Should().Contain(new[]
        {
            "AuthService", "MedicalDataService", "SedentaryEngineService",
            "GamificationService", "NotificationService", "MlPredictionService",
            "OrganizationService", "ReportingService"
        });
    }

    [Fact]
    public async Task Handle_GetGeneralHealthQuery_AllComponentsHealthy()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralHealthQuery(), CancellationToken.None);

        // Assert
        result.Value.ComponentHealth.Values.Should().OnlyContain(v => v == "Healthy");
    }

    [Fact]
    public async Task Handle_GetGeneralVersionQuery_ReturnsVersion()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralVersionQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task Handle_GetGeneralVersionQuery_BuildNumberFormat()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralVersionQuery(), CancellationToken.None);

        // Assert
        result.Value.BuildNumber.Should().Match("1.0.0.????????");
    }

    [Fact]
    public async Task Handle_GetGeneralVersionQuery_CommitHashNonEmpty()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralVersionQuery(), CancellationToken.None);

        // Assert
        result.Value.CommitHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_GetGeneralVersionQuery_VersionSemverFormat()
    {
        // Act
        var result = await _handler.Handle(new GetGeneralVersionQuery(), CancellationToken.None);

        // Assert
        result.Value.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    [Fact]
    public async Task Handle_GetGeneralVersionQuery_DoesNotCallReportingClient()
    {
        // Act
        await _handler.Handle(new GetGeneralVersionQuery(), CancellationToken.None);

        // Assert
        await _reportingClient.DidNotReceive().GetSystemMetricsAsync(Arg.Any<CancellationToken>());
    }
}
