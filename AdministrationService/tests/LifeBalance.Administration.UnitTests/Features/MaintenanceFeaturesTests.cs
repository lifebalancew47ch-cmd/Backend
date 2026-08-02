using FluentAssertions;
using LifeBalance.Administration.Application.Features.Maintenance;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class MaintenanceFeaturesTests
{
    private readonly Mock<IRepository<MaintenanceMode>> _repo = new();

    private MaintenanceCommandHandler CreateCommandHandler() => new(_repo.Object);
    private MaintenanceQueryHandler CreateQueryHandler() => new(_repo.Object);

    [Fact]
    public void Validator_RequiresFutureScheduledEnd()
    {
        var validator = new SetMaintenanceModeCommandValidator();

        var invalid = validator.Validate(new SetMaintenanceModeCommand(true, "msg", DateTime.UtcNow.AddMinutes(-5)));
        invalid.IsValid.Should().BeFalse();

        var valid = validator.Validate(new SetMaintenanceModeCommand(true, "msg", DateTime.UtcNow.AddHours(1)));
        valid.IsValid.Should().BeTrue();

        var disable = validator.Validate(new SetMaintenanceModeCommand(false));
        disable.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Command_Enable_CreatesSingletonWhenMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(MaintenanceMode.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaintenanceMode?)null);

        var handler = CreateCommandHandler();
        var result = await handler.Handle(
            new SetMaintenanceModeCommand(true, "Maintenance window", ByUserId: "admin-1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsEnabled.Should().BeTrue();
        result.Data.EnabledBy.Should().Be("admin-1");
        _repo.Verify(r => r.AddAsync(It.IsAny<MaintenanceMode>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<MaintenanceMode>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Command_Disable_TogglesOff()
    {
        var mode = MaintenanceMode.CreateDefault();
        mode.Enable("msg", "admin-1");
        _repo.Setup(r => r.GetByIdAsync(MaintenanceMode.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mode);

        var handler = CreateCommandHandler();
        var result = await handler.Handle(new SetMaintenanceModeCommand(false, ByUserId: "admin-2"), CancellationToken.None);

        result.Data!.IsEnabled.Should().BeFalse();
        result.Data.DisabledBy.Should().Be("admin-2");
    }

    [Fact]
    public async Task Query_ReturnsDefaultWhenMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(MaintenanceMode.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaintenanceMode?)null);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetMaintenanceStatusQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsEnabled.Should().BeFalse();
    }
}
