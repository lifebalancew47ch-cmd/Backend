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

public class GetAllPermissionsQueryHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetAllPermissionsQueryHandler>> _loggerMock = new();

    private GetAllPermissionsQueryHandler CreateHandler() => new(
        _permissionRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static PermissionDto CreatePermissionDto(string id, string name) =>
        new(id, name, null, "Profile", DateTime.UtcNow);

    [Fact]
    public async Task Handle_ReturnsMappedPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            new() { Id = "perm-1", Name = "profile:read" },
            new() { Id = "perm-2", Name = "profile:write" }
        };

        _permissionRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns<Permission>(p => CreatePermissionDto(p.Id, p.Name));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Should().Contain(dto => dto.Id == "perm-1" && dto.Name == "profile:read");
        result.Data.Should().Contain(dto => dto.Id == "perm-2" && dto.Name == "profile:write");
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryGetAllOnce()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission>());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsEveryPermissionToDto()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            new() { Id = "perm-1", Name = "profile:read" },
            new() { Id = "perm-2", Name = "profile:write" }
        };

        _permissionRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto("mapped", "Mapped"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(2);
        result.Data!.All(dto => dto.Name == "Mapped").Should().BeTrue();
        _mapperMock.Verify(m => m.Map<PermissionDto>(It.IsAny<Permission>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ReturnsSuccessResponseWith200()
    {
        // Arrange
        _permissionRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { new() { Id = "perm-1", Name = "profile:read" } });
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto("perm-1", "profile:read"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Operation successful.");
    }
}
