using Auth.Application.Commands.Roles;
using Auth.Application.Handlers.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Roles;

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILogger<DeleteRoleCommandHandler>> _loggerMock = new();

    private DeleteRoleCommandHandler CreateHandler() => new(
        _roleRepositoryMock.Object,
        _auditServiceMock.Object,
        _loggerMock.Object
    );

    private static Role CreateRole() => new()
    {
        Id = "role-1",
        Name = "Manager",
        NormalizedName = "MANAGER"
    };

    [Fact]
    public async Task Handle_ExistingRole_DeletesAndReturnsTrue()
    {
        // Arrange
        var role = CreateRole();

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeleteRoleCommand("role-1"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Contain("Role deleted successfully");
        _roleRepositoryMock.Verify(r => r.DeleteAsync("role-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentRole_Returns404Failure()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetByIdAsync("ghost-role", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeleteRoleCommand("ghost-role"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Role not found");
        _roleRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingRole_CallsDeleteAsyncWithId()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRole());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeleteRoleCommand("role-1"), CancellationToken.None);

        // Assert
        _roleRepositoryMock.Verify(r => r.DeleteAsync("role-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingRole_AuditsRoleChange()
    {
        // Arrange
        var role = CreateRole();

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeleteRoleCommand("role-1"), CancellationToken.None);

        // Assert
        _auditServiceMock.Verify(a => a.LogEventAsync(null, AuthEventType.RoleChange,
            "Role deleted: role-1", null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentRole_DoesNotAudit()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetByIdAsync("ghost-role", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeleteRoleCommand("ghost-role"), CancellationToken.None);

        // Assert
        _auditServiceMock.Verify(a => a.LogEventAsync(It.IsAny<string?>(), It.IsAny<AuthEventType>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
