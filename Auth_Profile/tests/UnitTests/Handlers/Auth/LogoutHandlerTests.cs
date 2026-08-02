using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.Handlers.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Auth;

public class LogoutHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILogger<LogoutHandler>> _loggerMock = new();

    private LogoutHandler CreateHandler() => new(
        _refreshTokenRepositoryMock.Object,
        _auditServiceMock.Object,
        _loggerMock.Object
    );

    [Fact]
    public async Task Handle_WithActiveRefreshToken_RevokesTokenAndLogsAudit()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest("refresh-token-123"), "user-123");
        var existingToken = new RefreshToken
        {
            Token = "refresh-token-123",
            UserId = "user-123",
            IsActive = true
        };

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("refresh-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        existingToken.IsActive.Should().BeFalse();
        existingToken.RevokedAt.Should().NotBeNull();
        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(existingToken, It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.Logout,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAlreadyInactiveToken_DoesNotUpdateButSucceeds()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest("refresh-token-123"), "user-123");
        var existingToken = new RefreshToken
        {
            Token = "refresh-token-123",
            UserId = "user-123",
            IsActive = false,
            RevokedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("refresh-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditServiceMock.Verify(a => a.LogEventAsync(It.IsAny<string?>(), It.IsAny<AuthEventType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonexistentToken_IsIdempotentAndSucceeds()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest("unknown-token"), "user-123");

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("unknown-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutTokenButWithUserId_RevokesAllSessions()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest(), "user-123");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllByUserIdAsync("user-123", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.Logout,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutTokenAndWithoutUserId_IsNoOpAndSucceeds()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllByUserIdAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokenRepositoryMock.Verify(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
