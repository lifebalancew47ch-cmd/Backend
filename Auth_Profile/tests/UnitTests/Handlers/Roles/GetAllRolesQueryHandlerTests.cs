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

public class GetAllRolesQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetAllRolesQueryHandler>> _loggerMock = new();

    private GetAllRolesQueryHandler CreateHandler() => new(
        _roleRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static RoleDto CreateRoleDto(string id, string name) =>
        new(id, name, null, new List<string>(), DateTime.UtcNow);

    [Fact]
    public async Task Handle_ReturnsMappedRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new() { Id = "role-1", Name = "Admin" },
            new() { Id = "role-2", Name = "User" }
        };

        _roleRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns<Role>(r => CreateRoleDto(r.Id, r.Name));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Should().Contain(dto => dto.Id == "role-1" && dto.Name == "Admin");
        result.Data.Should().Contain(dto => dto.Id == "role-2" && dto.Name == "User");
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryGetAllOnce()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // Assert
        _roleRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsEveryRoleToDto()
    {
        // Arrange
        var roles = new List<Role>
        {
            new() { Id = "role-1", Name = "Admin" },
            new() { Id = "role-2", Name = "User" }
        };

        _roleRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto("mapped", "Mapped"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(2);
        result.Data!.All(dto => dto.Name == "Mapped").Should().BeTrue();
        _mapperMock.Verify(m => m.Map<RoleDto>(It.IsAny<Role>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ReturnsSuccessResponseWith200()
    {
        // Arrange
        _roleRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { new() { Id = "role-1", Name = "Admin" } });
        _mapperMock.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(CreateRoleDto("role-1", "Admin"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Operation successful.");
    }
}
