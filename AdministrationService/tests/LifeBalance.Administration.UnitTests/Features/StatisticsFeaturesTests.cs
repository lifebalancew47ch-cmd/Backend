using FluentAssertions;
using LifeBalance.Administration.Application.Features.Statistics;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class StatisticsFeaturesTests
{
    private readonly Mock<IRepository<Catalog>> _catalogRepo = new();
    private readonly Mock<IRepository<SystemParameter>> _parameterRepo = new();
    private readonly Mock<IRepository<FeatureFlag>> _flagRepo = new();
    private readonly Mock<IRepository<AuditLog>> _auditRepo = new();
    private readonly Mock<IRepository<SystemLog>> _logRepo = new();
    private readonly Mock<IRepository<MaintenanceMode>> _maintenanceRepo = new();
    private readonly Mock<IServiceStatusService> _statusService = new();

    private AdministrativeStatisticsQueryHandler CreateHandler() => new(
        _catalogRepo.Object, _parameterRepo.Object, _flagRepo.Object,
        _auditRepo.Object, _logRepo.Object, _maintenanceRepo.Object, _statusService.Object);

    [Fact]
    public async Task Handle_AggregatesCountersAndBoard()
    {
        _catalogRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Catalog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _parameterRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SystemParameter, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _flagRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FeatureFlag, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _auditRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);
        _logRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SystemLog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(200);
        _maintenanceRepo.Setup(r => r.GetByIdAsync(MaintenanceMode.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MaintenanceMode.CreateDefault());
        _statusService.Setup(s => s.GetBoardAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ServiceStatusSnapshot(MicroserviceName.Auth, "Auth", ServiceHealthStatus.Healthy, 200, "OK", 10, null, null, DateTime.UtcNow, DateTime.UtcNow),
                new ServiceStatusSnapshot(MicroserviceName.Dashboard, "Dashboard", ServiceHealthStatus.Unhealthy, 503, "Down", 10, null, null, DateTime.UtcNow, null)
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAdministrativeStatisticsQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.TotalCatalogs.Should().Be(3);
        result.Data.TotalParameters.Should().Be(5);
        result.Data.TotalAuditEntries.Should().Be(100);
        result.Data.TotalLogs.Should().Be(200);
        result.Data.TotalServices.Should().Be(2);
        result.Data.HealthyServices.Should().Be(1);
        result.Data.UnhealthyServices.Should().Be(1);
    }
}
