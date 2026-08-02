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

public class GetLoginHistoryQueryHandlerTests
{
    private readonly Mock<ILoginHistoryRepository> _loginHistoryRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetLoginHistoryQueryHandler>> _loggerMock = new();

    private GetLoginHistoryQueryHandler CreateHandler() => new(
        _loginHistoryRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object
    );

    private static LoginHistory CreateHistory(string id, string email) => new()
    {
        Id = id,
        UserId = "user-123",
        Email = email,
        IpAddress = "127.0.0.1",
        UserAgent = "UnitTests",
        Device = "Desktop",
        Success = true,
        LoginAt = DateTime.UtcNow
    };

    private static LoginHistoryDto CreateHistoryDto(string id, string email) =>
        new(id, email, "127.0.0.1", "UnitTests", "Desktop", true, null, DateTime.UtcNow);

    [Fact]
    public async Task Handle_NoUserId_ReturnsAllHistoryPaginated()
    {
        // Arrange
        var items = new List<LoginHistory> { CreateHistory("h-1", "a@example.com"), CreateHistory("h-2", "b@example.com") };

        _loginHistoryRepositoryMock.Setup(r => r.GetAllAsync(2, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        _loginHistoryRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);
        _mapperMock.Setup(m => m.Map<LoginHistoryDto>(It.IsAny<LoginHistory>()))
            .Returns<LoginHistory>(h => CreateHistoryDto(h.Id, h.Email));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetLoginHistoryQuery(null, 2, 20), CancellationToken.None);

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
        _loginHistoryRepositoryMock.Verify(r => r.GetAllAsync(2, 20, It.IsAny<CancellationToken>()), Times.Once);
        _loginHistoryRepositoryMock.Verify(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUserId_ReturnsUserHistory()
    {
        // Arrange
        var items = new List<LoginHistory> { CreateHistory("h-1", "a@example.com") };

        _loginHistoryRepositoryMock.Setup(r => r.GetByUserIdAsync("user-123", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        _loginHistoryRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _mapperMock.Setup(m => m.Map<LoginHistoryDto>(It.IsAny<LoginHistory>()))
            .Returns<LoginHistory>(h => CreateHistoryDto(h.Id, h.Email));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetLoginHistoryQuery("user-123", 1, 10), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.HasPrevious.Should().BeFalse();
        result.Data.HasNext.Should().BeFalse();
        _loginHistoryRepositoryMock.Verify(r => r.GetByUserIdAsync("user-123", 1, 10, It.IsAny<CancellationToken>()), Times.Once);
        _loginHistoryRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPagedResult()
    {
        // Arrange
        _loginHistoryRepositoryMock.Setup(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoginHistory>());
        _loginHistoryRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetLoginHistoryQuery(null, 1, 20), CancellationToken.None);

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
        _loginHistoryRepositoryMock.Setup(r => r.GetAllAsync(3, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoginHistory>());
        _loginHistoryRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(23L);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetLoginHistoryQuery(null, 3, 5), CancellationToken.None);

        // Assert
        result.Data!.Page.Should().Be(3);
        result.Data.PageSize.Should().Be(5);
        result.Data.TotalPages.Should().Be(5);
        result.Data.HasPrevious.Should().BeTrue();
        result.Data.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MapsHistoryToDtos()
    {
        // Arrange
        var item = CreateHistory("h-1", "a@example.com");

        _loginHistoryRepositoryMock.Setup(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoginHistory> { item });
        _loginHistoryRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _mapperMock.Setup(m => m.Map<LoginHistoryDto>(item))
            .Returns(CreateHistoryDto("h-1", "a@example.com"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetLoginHistoryQuery(null, 1, 20), CancellationToken.None);

        // Assert
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Id.Should().Be("h-1");
        result.Data.Items.First().Email.Should().Be("a@example.com");
        result.Data.Items.First().IpAddress.Should().Be("127.0.0.1");
        result.Data.Items.First().Device.Should().Be("Desktop");
        result.Data.Items.First().Success.Should().BeTrue();
    }
}
