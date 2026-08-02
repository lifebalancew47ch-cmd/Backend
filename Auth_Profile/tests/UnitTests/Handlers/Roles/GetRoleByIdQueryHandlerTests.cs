using Auth.Application.DTOs.Roles;
using Auth.Application.Handlers.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Roles;
using Auth.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Roles;

public class GetRoleByIdQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetRoleByIdQueryHandler>> _loggerMock = new();

    private GetRoleByIdQueryHandler CreateHandler() => new(
        _roleRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static Role CreateRole() => new()
    {
        Id = "role-1",
        Name = "Admin",
        NormalizedName = "ADMIN",
        Description = "Administrator role",
        PermissionIds = new List<string> { "perm-1" }
    };

    private static RoleDto CreateRoleDto() =>
        new("role-1", "Admin", "Administrator role", new List<string> { "perm-1" }, DateTime.UtcNow);

    [Fact]
    public async Task Handle_ExistingRole_ReturnsMappedRole()
    {
        // Arrange
        var role = CreateRole();
        var dto = CreateRoleDto();

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _mapperMock.Setup(m => m.Map<RoleDto>(role))
            .Returns(dto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetRoleByIdQuery("role-1"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data.Should().Be(dto);
    }

    [Fact]
    public async Task Handle_NonexistentRole_Returns404Failure()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetByIdAsync("ghost-role", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetRoleByIdQuery("ghost-role"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Role not found");
    }

    [Fact]
    public async Task Handle_ExistingRole_CallsRepositoryWithId()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRole());
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetRoleByIdQuery("role-1"), CancellationToken.None);

        // Assert
        _roleRepositoryMock.Verify(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentRole_DoesNotMap()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetByIdAsync("ghost-role", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetRoleByIdQuery("ghost-role"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mapperMock.Verify(m => m.Map<RoleDto>(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingRole_ReturnsRoleWithCorrectFields()
    {
        // Arrange
        var role = CreateRole();

        _roleRepositoryMock.Setup(r => r.GetByIdAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetRoleByIdQuery("role-1"), CancellationToken.None);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be("role-1");
        result.Data.Name.Should().Be("Admin");
        result.Data.Description.Should().Be("Administrator role");
        result.Data.PermissionIds.Should().Contain("perm-1");
    }
}
