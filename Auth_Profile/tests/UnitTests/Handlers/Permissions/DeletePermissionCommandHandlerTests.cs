using Auth.Application.Commands.Permissions;
using Auth.Application.Handlers.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Permissions;

public class DeletePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly Mock<ILogger<DeletePermissionCommandHandler>> _loggerMock = new();

    private DeletePermissionCommandHandler CreateHandler() => new(
        _permissionRepositoryMock.Object,
        _loggerMock.Object
    );

    private static Permission CreatePermission() => new()
    {
        Id = "perm-1",
        Name = "profile:read",
        NormalizedName = "PROFILE:READ",
        Module = "Profile"
    };

    [Fact]
    public async Task Handle_ExistingPermission_DeletesAndReturnsTrue()
    {
        // Arrange
        var permission = CreatePermission();

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeletePermissionCommand("perm-1"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Contain("Permission deleted successfully");
        _permissionRepositoryMock.Verify(r => r.DeleteAsync("perm-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentPermission_Returns404Failure()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("ghost-perm", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeletePermissionCommand("ghost-perm"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Permission not found");
        _permissionRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingPermission_CallsDeleteAsyncWithId()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePermission());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeletePermissionCommand("perm-1"), CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.DeleteAsync("perm-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentPermission_DoesNotCallDelete()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("ghost-perm", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeletePermissionCommand("ghost-perm"), CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingPermission_DeletesSoftDeletedEntity()
    {
        // Arrange
        var permission = CreatePermission();

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeletePermissionCommand("perm-1"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        _permissionRepositoryMock.Verify(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
