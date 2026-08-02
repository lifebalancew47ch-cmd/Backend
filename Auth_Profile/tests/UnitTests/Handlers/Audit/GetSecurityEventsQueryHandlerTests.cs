using Auth.Application.DTOs.Audit;
using Auth.Application.Handlers.Audit;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Audit;
using Auth.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Audit;

public class GetSecurityEventsQueryHandlerTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetSecurityEventsQueryHandler>> _loggerMock = new();

    private GetSecurityEventsQueryHandler CreateHandler() => new(
        _auditLogRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static AuditLog CreateLog(string id, string action) => new()
    {
        Id = id,
        UserId = "user-123",
        Action = action,
        Details = "details",
        IpAddress = "127.0.0.1",
        ResourceType = "Profile",
        Success = true,
        CreatedAt = DateTime.UtcNow
    };

    private static AuditLogDto CreateLogDto(string id, string action) =>
        new(id, "user-123", action, "details", "127.0.0.1", "Profile", true, null, DateTime.UtcNow);

    [Fact]
    public async Task Handle_ReturnsAllEventsPaginated()
    {
        // Arrange
        var items = new List<AuditLog> { CreateLog("l-1", "ProfileUpdate"), CreateLog("l-2", "Login") };

        _auditLogRepositoryMock.Setup(r => r.GetAllAsync(2, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        _auditLogRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);
        _mapperMock.Setup(m => m.Map<AuditLogDto>(It.IsAny<AuditLog>()))
            .Returns<AuditLog>(l => CreateLogDto(l.Id, l.Action));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSecurityEventsQuery(2, 20), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Page.Should().Be(2);
        result.Data.PageSize.Should().Be(20);
        result.Data.TotalCount.Should().Be(42);
        result.Data.TotalPages.Should().Be(3);
        result.Data.HasPrevious.Should().BeTrue();
        result.Data.HasNext.Should().BeTrue();
        _auditLogRepositoryMock.Verify(r => r.GetAllAsync(2, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPagedResult()
    {
        // Arrange
        _auditLogRepositoryMock.Setup(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLog>());
        _auditLogRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSecurityEventsQuery(1, 20), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
        result.Data.TotalPages.Should().Be(0);
        result.Data.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SetsPaginationMetadata()
    {
        // Arrange
        _auditLogRepositoryMock.Setup(r => r.GetAllAsync(3, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLog>());
        _auditLogRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(23L);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSecurityEventsQuery(3, 5), CancellationToken.None);

        // Assert
        result.Data!.Page.Should().Be(3);
        result.Data.PageSize.Should().Be(5);
        result.Data.TotalPages.Should().Be(5);
        result.Data.HasPrevious.Should().BeTrue();
        result.Data.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MapsLogsToDtos()
    {
        // Arrange
        var item = CreateLog("l-1", "ProfileUpdate");

        _auditLogRepositoryMock.Setup(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLog> { item });
        _auditLogRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _mapperMock.Setup(m => m.Map<AuditLogDto>(item))
            .Returns(CreateLogDto("l-1", "ProfileUpdate"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSecurityEventsQuery(1, 20), CancellationToken.None);

        // Assert
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Id.Should().Be("l-1");
        result.Data.Items.First().Action.Should().Be("ProfileUpdate");
        result.Data.Items.First().UserId.Should().Be("user-123");
        result.Data.Items.First().ResourceType.Should().Be("Profile");
        result.Data.Items.First().Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CallsCountAsyncOnce()
    {
        // Arrange
        _auditLogRepositoryMock.Setup(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLog>());
        _auditLogRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5L);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetSecurityEventsQuery(1, 20), CancellationToken.None);

        // Assert
        _auditLogRepositoryMock.Verify(r => r.CountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
