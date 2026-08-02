using Auth.Application.Commands.Permissions;
using Auth.Application.DTOs.Permissions;
using Auth.Application.Handlers.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Permissions;

public class UpdatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<UpdatePermissionCommandHandler>> _loggerMock = new();

    private UpdatePermissionCommandHandler CreateHandler() => new(
        _permissionRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static Permission CreatePermission() => new()
    {
        Id = "perm-1",
        Name = "profile:read",
        NormalizedName = "PROFILE:READ",
        Description = "Old description",
        Module = "Profile"
    };

    private static PermissionDto CreatePermissionDto() =>
        new("perm-1", "profile:write", "Write profile", "Profile", DateTime.UtcNow);

    private static UpdatePermissionRequest CreateRequest() =>
        new("profile:write", "Write profile", "Profile");

    [Fact]
    public async Task Handle_ExistingPermission_UpdatesAndReturnsSuccess()
    {
        // Arrange
        var permission = CreatePermission();
        var command = new UpdatePermissionCommand("perm-1", CreateRequest());

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("profile:write", "perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        permission.Name.Should().Be("profile:write");
        permission.NormalizedName.Should().Be("PROFILE:WRITE");
        permission.Description.Should().Be("Write profile");
        permission.Module.Should().Be("Profile");
        permission.UpdatedAt.Should().NotBeNull();
        _permissionRepositoryMock.Verify(r => r.UpdateAsync(permission, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentPermission_Returns404Failure()
    {
        // Arrange
        var command = new UpdatePermissionCommand("ghost-perm", CreateRequest());

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("ghost-perm", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Permission not found");
        _permissionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateNameExcludingSelf_Returns400Failure()
    {
        // Arrange
        var permission = CreatePermission();
        var command = new UpdatePermissionCommand("perm-1", CreateRequest());

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("profile:write", "perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Permission already exists");
        permission.Name.Should().Be("profile:read");
        _permissionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingPermission_NormalizesUpdatedName()
    {
        // Arrange
        var permission = CreatePermission();
        var command = new UpdatePermissionCommand("perm-1",
            new UpdatePermissionRequest("roles:read", "Read roles", "Roles"));

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("roles:read", "perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Permission>(p =>
            p.Name == "roles:read" &&
            p.NormalizedName == "ROLES:READ" &&
            p.Module == "Roles"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingPermission_PersistsUpdatedPermission()
    {
        // Arrange
        var permission = CreatePermission();
        var command = new UpdatePermissionCommand("perm-1", CreateRequest());

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("profile:write", "perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Permission>(p =>
            p.Id == "perm-1" &&
            p.Name == "profile:write" &&
            p.Description == "Write profile"),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Data.Should().NotBeNull();
    }
}
