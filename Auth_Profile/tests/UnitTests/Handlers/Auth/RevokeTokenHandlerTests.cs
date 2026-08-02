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

public class RevokeTokenHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILogger<RevokeTokenHandler>> _loggerMock = new();

    private RevokeTokenHandler CreateHandler() => new(
        _refreshTokenRepositoryMock.Object,
        _auditServiceMock.Object,
        _loggerMock.Object
    );

    private static RefreshToken CreateActiveToken() => new()
    {
        Id = "rt-1",
        Token = "refresh-token-123",
        UserId = "user-123",
        IsActive = true,
        ExpiresAt = DateTime.UtcNow.AddDays(1)
    };

    [Fact]
    public async Task Handle_ActiveToken_RevokesAndLogsAudit()
    {
        // Arrange
        var command = new RevokeTokenCommand(new TokenRevocationRequest("refresh-token-123"));
        var token = CreateActiveToken();

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("refresh-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.TokenRevocation,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentToken_ReturnsFailure()
    {
        // Arrange
        var command = new RevokeTokenCommand(new TokenRevocationRequest("unknown-token"));

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("unknown-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("not found");
        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ReturnsFailure()
    {
        // Arrange
        var command = new RevokeTokenCommand(new TokenRevocationRequest("refresh-token-123"));
        var token = CreateActiveToken();
        token.IsActive = false;
        token.RevokedAt = DateTime.UtcNow.AddMinutes(-10);

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("refresh-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already revoked or expired");
        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var command = new RevokeTokenCommand(new TokenRevocationRequest("refresh-token-123"));
        var token = CreateActiveToken();
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("refresh-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already revoked or expired");
        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ActiveToken_DoesNotRevokeOtherSessions()
    {
        // Arrange
        var command = new RevokeTokenCommand(new TokenRevocationRequest("refresh-token-123"));
        var token = CreateActiveToken();

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("refresh-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllByUserIdAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
