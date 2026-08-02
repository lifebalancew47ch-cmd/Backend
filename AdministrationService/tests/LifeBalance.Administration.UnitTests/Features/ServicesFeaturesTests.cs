using FluentAssertions;
using LifeBalance.Administration.Application.Features.Services;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class ServicesFeaturesTests
{
    private readonly Mock<IServiceStatusService> _statusService = new();

    private ServicesQueryHandler CreateQueryHandler() => new(_statusService.Object);

    private static ServiceStatusSnapshot CreateSnapshot(MicroserviceName service, ServiceHealthStatus status)
        => new(service, service.ToString(), status, 200, "OK", 50, "1.0.0", null, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task GetBoard_CountsHealthStates()
    {
        var snapshots = new[]
        {
            CreateSnapshot(MicroserviceName.Auth, ServiceHealthStatus.Healthy),
            CreateSnapshot(MicroserviceName.Organization, ServiceHealthStatus.Healthy),
            CreateSnapshot(MicroserviceName.Notifications, ServiceHealthStatus.Unhealthy),
            CreateSnapshot(MicroserviceName.MedicalData, ServiceHealthStatus.Degraded),
            CreateSnapshot(MicroserviceName.SedentaryEngine, ServiceHealthStatus.Unknown)
        };
        _statusService.Setup(s => s.GetBoardAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(snapshots);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetServicesStatusQuery(), CancellationToken.None);

        result.Data!.Total.Should().Be(5);
        result.Data.Healthy.Should().Be(2);
        result.Data.Unhealthy.Should().Be(1);
        result.Data.Degraded.Should().Be(1);
        result.Data.Unknown.Should().Be(1);
    }

    [Fact]
    public async Task GetService_ForwardsForceRefresh()
    {
        _statusService.Setup(s => s.GetServiceAsync(MicroserviceName.Dashboard, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSnapshot(MicroserviceName.Dashboard, ServiceHealthStatus.Healthy));

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetServiceStatusQuery(MicroserviceName.Dashboard, true), CancellationToken.None);

        result.Data!.Service.Should().Be(MicroserviceName.Dashboard);
        result.Data.Status.Should().Be(ServiceHealthStatus.Healthy);
        _statusService.Verify(s => s.GetServiceAsync(MicroserviceName.Dashboard, true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
