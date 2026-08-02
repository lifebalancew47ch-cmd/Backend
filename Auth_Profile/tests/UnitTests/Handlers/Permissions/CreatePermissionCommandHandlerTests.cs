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

public class CreatePermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<CreatePermissionCommandHandler>> _loggerMock = new();

    private CreatePermissionCommandHandler CreateHandler() => new(
        _permissionRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static PermissionDto CreatePermissionDto() =>
        new("perm-1", "profile:read", "Read profile", "Profile", DateTime.UtcNow);

    [Fact]
    public async Task Handle_UniqueName_CreatesPermissionAndReturns201()
    {
        // Arrange
        var command = new CreatePermissionCommand(new CreatePermissionRequest("profile:read", "Read profile", "Profile"));

        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("profile:read", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        _permissionRepositoryMock.Verify(r => r.AddAsync(It.Is<Permission>(p =>
            p.Name == "profile:read" &&
            p.NormalizedName == "PROFILE:READ" &&
            p.Description == "Read profile" &&
            p.Module == "Profile"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_Returns400Failure()
    {
        // Arrange
        var command = new CreatePermissionCommand(new CreatePermissionRequest("profile:read", null, "Profile"));

        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("profile:read", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Permission already exists");
        _permissionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UniqueName_CreatesPermissionWithNormalizedName()
    {
        // Arrange
        var command = new CreatePermissionCommand(new CreatePermissionRequest("audit:read", null, "Audit"));

        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("audit:read", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.AddAsync(It.Is<Permission>(p =>
            p.NormalizedName == "AUDIT:READ"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UniqueName_CallsAddAsyncOnce()
    {
        // Arrange
        var command = new CreatePermissionCommand(new CreatePermissionRequest("profile:read", null, "Profile"));

        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("profile:read", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(CreatePermissionDto());

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UniqueName_ReturnsMappedDto()
    {
        // Arrange
        var dto = CreatePermissionDto();
        var command = new CreatePermissionCommand(new CreatePermissionRequest("profile:read", "Read profile", "Profile"));

        _permissionRepositoryMock.Setup(r => r.ExistsByNameAsync("profile:read", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(dto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Data.Should().Be(dto);
        result.Data!.Name.Should().Be("profile:read");
        result.Data.Module.Should().Be("Profile");
    }
}
