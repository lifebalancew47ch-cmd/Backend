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

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<CreateRoleCommandHandler>> _loggerMock = new();

    private CreateRoleCommandHandler CreateHandler() => new(
        _roleRepositoryMock.Object,
        _permissionRepositoryMock.Object,
        _auditServiceMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static RoleDto CreateRoleDto() =>
        new("role-1", "Manager", "Manager role", new List<string>(), DateTime.UtcNow);

    [Fact]
    public async Task Handle_UniqueName_CreatesRoleAndReturns201()
    {
        // Arrange
        var command = new CreateRoleCommand(new CreateRoleRequest("Manager", "Manager role", null));

        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Manager", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        _roleRepositoryMock.Verify(r => r.AddAsync(It.Is<Role>(role =>
            role.Name == "Manager" &&
            role.NormalizedName == "MANAGER" &&
            role.Description == "Manager role" &&
            role.PermissionIds.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_Returns400Failure()
    {
        // Arrange
        var command = new CreateRoleCommand(new CreateRoleRequest("Manager", null, null));

        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Manager", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Role already exists");
        _roleRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidPermissions_Returns400Failure()
    {
        // Arrange
        var command = new CreateRoleCommand(new CreateRoleRequest("Manager", null, new List<string> { "perm-1", "perm-2" }));

        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Manager", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _permissionRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { new() { Id = "perm-1" } });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("permissions do not exist");
        _roleRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidPermissions_AssignsPermissionIds()
    {
        // Arrange
        var command = new CreateRoleCommand(new CreateRoleRequest("Manager", null, new List<string> { "perm-1", "perm-2" }));

        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Manager", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _permissionRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { new() { Id = "perm-1" }, new() { Id = "perm-2" } });
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _roleRepositoryMock.Verify(r => r.AddAsync(It.Is<Role>(role =>
            role.PermissionIds.SequenceEqual(new[] { "perm-1", "perm-2" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatedRole_AuditsRoleChange()
    {
        // Arrange
        var command = new CreateRoleCommand(new CreateRoleRequest("Manager", null, null));

        _roleRepositoryMock.Setup(r => r.ExistsByNameAsync("Manager", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _auditServiceMock.Verify(a => a.LogEventAsync(null, AuthEventType.RoleChange,
            "Role created: Manager", null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
