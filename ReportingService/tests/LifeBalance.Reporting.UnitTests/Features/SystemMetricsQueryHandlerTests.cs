using FluentAssertions;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Application.Features.SystemMetrics;
using NSubstitute;

namespace LifeBalance.Reporting.UnitTests.Features;

public class SystemMetricsQueryHandlerTests
{
    private readonly IHealthProbeService _healthProbe = Substitute.For<IHealthProbeService>();
    private readonly IOrganizationServiceClient _organizationClient = Substitute.For<IOrganizationServiceClient>();
    private readonly IMedicalDataServiceClient _medicalClient = Substitute.For<IMedicalDataServiceClient>();
    private readonly IReportGenerationLogService _logService = Substitute.For<IReportGenerationLogService>();
    private readonly GetSystemMetricsQueryHandler _handler;

    public SystemMetricsQueryHandlerTests()
    {
        _handler = new GetSystemMetricsQueryHandler(_healthProbe, _organizationClient, _medicalClient, _logService);
    }

    [Fact]
    public async Task Handle_AllUpstreamsAvailable_ReturnsMetrics()
    {
        _healthProbe.GetPlatformHealthPercentageAsync(Arg.Any<CancellationToken>()).Returns(99.5);
        _organizationClient.GetPlatformStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new PlatformStatsDto(5000, 120, 300, 4800));
        _medicalClient.GetDailyActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new DailyActiveUsersDto(1500, DateTime.UtcNow));

        var result = await _handler.Handle(new GetSystemMetricsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalUsers.Should().Be(5000);
        result.Value.ActiveUsersToday.Should().Be(1500);
        result.Value.PlatformHealthPercentage.Should().Be(99.5);
        result.Value.SystemVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_OrganizationDown_ThrowsUpstreamUnavailable()
    {
        _healthProbe.GetPlatformHealthPercentageAsync(Arg.Any<CancellationToken>()).Returns(100);
        _organizationClient.GetPlatformStatsAsync(Arg.Any<CancellationToken>()).Returns((PlatformStatsDto?)null);
        _medicalClient.GetDailyActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new DailyActiveUsersDto(1, DateTime.UtcNow));

        await FluentActions.Awaiting(() => _handler.Handle(new GetSystemMetricsQuery(), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }

    [Fact]
    public async Task Handle_MedicalDown_ThrowsUpstreamUnavailable()
    {
        _healthProbe.GetPlatformHealthPercentageAsync(Arg.Any<CancellationToken>()).Returns(100);
        _organizationClient.GetPlatformStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new PlatformStatsDto(100, 10, 20, 90));
        _medicalClient.GetDailyActiveUsersAsync(Arg.Any<CancellationToken>()).Returns((DailyActiveUsersDto?)null);

        await FluentActions.Awaiting(() => _handler.Handle(new GetSystemMetricsQuery(), CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }
}
