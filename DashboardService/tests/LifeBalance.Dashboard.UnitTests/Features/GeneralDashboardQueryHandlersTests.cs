using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Exceptions;
using LifeBalance.Dashboard.Application.Features.GeneralDashboard;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class GeneralDashboardQueryHandlersTests
{
    private readonly IReportingServiceClient _reportingClient = Substitute.For<IReportingServiceClient>();
    private readonly HealthCheckService _healthCheckService = Substitute.For<HealthCheckService>();
    private readonly GeneralDashboardQueryHandlers _handler;

    public GeneralDashboardQueryHandlersTests()
    {
        _handler = new GeneralDashboardQueryHandlers(_reportingClient, _healthCheckService);
    }

    private static GeneralSystemMetricsDto CreateMetrics() =>
        new(5000, 1500, 99.9, "1.0.0");

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_ReturnsSummary()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(CreateMetrics());

        var query = new GetGeneralSummaryQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveUsers.Should().Be(1500);
        result.Value.GlobalHealthScore.Should().Be(99.9);
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_ReportingDown_ThrowsUpstreamUnavailable()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns((GeneralSystemMetricsDto?)null);

        // Act
        await FluentActions.Awaiting(() => _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
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
    public async Task Handle_GetGeneralSummaryQuery_SystemStatusHealthyWhenScoreHigh()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(CreateMetrics());

        // Act
        var result = await _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None);

        // Assert
        result.Value.SystemStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_SystemStatusDegradedWhenScoreLow()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new GeneralSystemMetricsDto(5000, 1500, 70.0, "1.0.0"));

        // Act
        var result = await _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None);

        // Assert
        result.Value.SystemStatus.Should().Be("Degraded");
    }

    [Fact]
    public async Task Handle_GetGeneralSummaryQuery_ReportingClient_InvokedOnce()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(CreateMetrics());

        // Act
        await _handler.Handle(new GetGeneralSummaryQuery(), CancellationToken.None);

        // Assert
        await _reportingClient.Received(1).GetSystemMetricsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetGeneralIndicatorsQuery_ThrowsWhenNoIndicatorsSource()
    {
        // Act
        await FluentActions.Awaiting(() => _handler.Handle(new GetGeneralIndicatorsQuery(), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetGeneralIndicatorsQuery_DoesNotCallReportingClient()
    {
        // Act
        await FluentActions.Awaiting(() => _handler.Handle(new GetGeneralIndicatorsQuery(), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();

        // Assert
        await _reportingClient.DidNotReceive().GetSystemMetricsAsync(Arg.Any<CancellationToken>());
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
    public async Task Handle_GetGeneralKpisQuery_ReportingDown_ThrowsUpstreamUnavailable()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns((GeneralSystemMetricsDto?)null);

        // Act
        await FluentActions.Awaiting(() => _handler.Handle(new GetGeneralKpisQuery(), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_GetGeneralKpisQuery_FamiliesAndCompanies_AreStructuralZeros()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(CreateMetrics());

        // Act
        var result = await _handler.Handle(new GetGeneralKpisQuery(), CancellationToken.None);

        // Assert
        result.Value.ActiveFamilies.Should().Be(0);
        result.Value.ActiveCompanies.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetGeneralKpisQuery_ReportingClient_InvokedOnce()
    {
        // Arrange
        _reportingClient.GetSystemMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(CreateMetrics());

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
    public async Task Handle_GetGeneralHealthQuery_ReturnsActualHealth()
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "DashboardService", new HealthReportEntry(HealthStatus.Healthy, "test", TimeSpan.Zero, null, null) },
            { "UpstreamServices", new HealthReportEntry(HealthStatus.Healthy, "test", TimeSpan.Zero, null, null) }
        };
        var report = new HealthReport(entries, TimeSpan.Zero);
        _healthCheckService.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(report));

        // Act
        var result = await _handler.Handle(new GetGeneralHealthQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("Healthy");
        result.Value.ComponentHealth.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetGeneralHealthQuery_DoesNotCallReportingClient()
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "DashboardService", new HealthReportEntry(HealthStatus.Healthy, "test", TimeSpan.Zero, null, null) }
        };
        var report = new HealthReport(entries, TimeSpan.Zero);
        _healthCheckService.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(report));

        // Act
        await _handler.Handle(new GetGeneralHealthQuery(), CancellationToken.None);

        // Assert
        await _reportingClient.DidNotReceive().GetSystemMetricsAsync(Arg.Any<CancellationToken>());
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
