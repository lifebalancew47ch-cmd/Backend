using Auth.Application.DTOs.Permissions;
using Auth.Application.Handlers.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Permissions;
using Auth.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Permissions;

public class GetPermissionByIdQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetPermissionByIdQueryHandler>> _loggerMock = new();

    private GetPermissionByIdQueryHandler CreateHandler() => new(
        _permissionRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static Permission CreatePermission() => new()
    {
        Id = "perm-1",
        Name = "profile:read",
        NormalizedName = "PROFILE:READ",
        Description = "Read profile",
        Module = "Profile"
    };

    private static PermissionDto CreatePermissionDto() =>
        new("perm-1", "profile:read", "Read profile", "Profile", DateTime.UtcNow);

    [Fact]
    public async Task Handle_ExistingPermission_ReturnsMappedPermission()
    {
        // Arrange
        var permission = CreatePermission();
        var dto = CreatePermissionDto();

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mapperMock.Setup(m => m.Map<PermissionDto>(permission))
            .Returns(dto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPermissionByIdQuery("perm-1"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data.Should().Be(dto);
    }

    [Fact]
    public async Task Handle_NonexistentPermission_Returns404Failure()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("ghost-perm", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPermissionByIdQuery("ghost-perm"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Permission not found");
    }

    [Fact]
    public async Task Handle_ExistingPermission_CallsRepositoryWithId()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePermission());
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetPermissionByIdQuery("perm-1"), CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentPermission_DoesNotMap()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("ghost-perm", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPermissionByIdQuery("ghost-perm"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mapperMock.Verify(m => m.Map<PermissionDto>(It.IsAny<Permission>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingPermission_ReturnsPermissionWithCorrectFields()
    {
        // Arrange
        var permission = CreatePermission();

        _permissionRepositoryMock.Setup(r => r.GetByIdAsync("perm-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPermissionByIdQuery("perm-1"), CancellationToken.None);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be("perm-1");
        result.Data.Name.Should().Be("profile:read");
        result.Data.Description.Should().Be("Read profile");
        result.Data.Module.Should().Be("Profile");
    }
}
