using Auth.Application.Commands.Roles;
using Auth.Application.DTOs.Roles;
using Auth.Application.Handlers.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Roles;

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<UpdateRoleCommandHandler>> _loggerMock = new();

    private UpdateRoleCommandHandler CreateHandler() => new(
        _roleRepositoryMock.Object,
        _permissionRepositoryMock.Object,
        _auditServiceMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static Role CreateRole() => new()
    {
        Id = "role-1",
        Name = "Manager",
        NormalizedName = "MANAGER",
        Description = "Old description",
        PermissionIds = new List<string> { "perm-1" }
    };

    private static RoleDto CreateRoleDto() =>
        new("role-1", "Supervisor", "Supervisor role", new List<string>(), DateTime.UtcNow);

    private static UpdateRoleRequest CreateRequest() =>
        new("Supervisor", "Supervisor role", null);

    [Fact]
    public async Task Handle_ExistingRole_UpdatesAndReturnsSuccess()
    {
        // Arrange
        var role = CreateRole();
        var command = new UpdateRoleCommand("role-1", CreateRequest());

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Supervisor", "role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        role.Name.Should().Be("Supervisor");
        role.NormalizedName.Should().Be("SUPERVISOR");
        role.Description.Should().Be("Supervisor role");
        role.PermissionIds.Should().BeEmpty();
        role.UpdatedAt.Should().NotBeNull();
        _roleRepositoryMock.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentRole_Returns404Failure()
    {
        // Arrange
        var command = new UpdateRoleCommand("ghost-role", CreateRequest());

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("ghost-role", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Role not found");
        _roleRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateNameExcludingSelf_Returns400Failure()
    {
        // Arrange
        var role = CreateRole();
        var command = new UpdateRoleCommand("role-1", CreateRequest());

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Supervisor", "role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Role already exists");
        _roleRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidPermissions_Returns400AndDoesNotUpdate()
    {
        // Arrange
        var role = CreateRole();
        var command = new UpdateRoleCommand("role-1",
            new UpdateRoleRequest("Supervisor", null, new List<string> { "perm-1", "perm-2" }));

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Supervisor", "role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _permissionRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { new() { Id = "perm-1" } });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permissions do not exist");
        role.Name.Should().Be("Manager");
        role.PermissionIds.Should().Contain("perm-1");
        _roleRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingRole_AuditsRoleChange()
    {
        // Arrange
        var role = CreateRole();
        var command = new UpdateRoleCommand("role-1", CreateRequest());

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Supervisor", "role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _auditServiceMock.Verify(a => a.LogEventAsync(null, AuthEventType.RoleChange,
            "Role updated: Supervisor", null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
