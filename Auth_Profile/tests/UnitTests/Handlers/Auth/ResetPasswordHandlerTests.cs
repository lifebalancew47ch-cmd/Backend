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

public class ResetPasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILogger<ResetPasswordHandler>> _loggerMock = new();

    private ResetPasswordHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenRepositoryMock.Object,
        _refreshTokenRepositoryMock.Object,
        _passwordServiceMock.Object,
        _auditServiceMock.Object,
        _loggerMock.Object
    );

    private static User CreateUser() => new()
    {
        Id = "user-123",
        Email = "test@example.com",
        PasswordHash = "old_hash",
        FailedLoginAttempts = 4,
        LockoutEnd = DateTime.UtcNow.AddMinutes(10)
    };

    private static PasswordResetToken CreateValidToken() => new()
    {
        Id = "prt-1",
        UserId = "user-123",
        Token = "reset-token-123",
        ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        IsUsed = false
    };

    [Fact]
    public async Task Handle_ValidToken_HashesNewPasswordMarksTokenUsedAndRevokesSessions()
    {
        // Arrange
        var command = new ResetPasswordCommand(new ResetPasswordRequest("test@example.com", "reset-token-123", "NewPass123!", "NewPass123!"));
        var user = CreateUser();
        var token = CreateValidToken();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("reset-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _passwordServiceMock.Setup(p => p.HashPassword("NewPass123!"))
            .Returns("new_hash");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        user.PasswordHash.Should().Be("new_hash");
        user.LastPasswordChangeAt.Should().NotBeNull();
        token.IsUsed.Should().BeTrue();
        token.UsedAt.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepositoryMock.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllByUserIdAsync("user-123", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.PasswordReset,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentUser_ReturnsFailureWithoutTokenLookup()
    {
        // Arrange
        var command = new ResetPasswordCommand(new ResetPasswordRequest("ghost@example.com", "any-token", "NewPass123!", "NewPass123!"));

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("ghost@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Invalid reset request");
        _tokenRepositoryMock.Verify(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand(new ResetPasswordRequest("test@example.com", "reset-token-123", "NewPass123!", "NewPass123!"));
        var user = CreateUser();
        var token = CreateValidToken();
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("reset-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired reset token");
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ReturnsFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand(new ResetPasswordRequest("test@example.com", "reset-token-123", "NewPass123!", "NewPass123!"));
        var user = CreateUser();
        var token = CreateValidToken();
        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow.AddHours(-1);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("reset-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired reset token");
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TokenBelongingToAnotherUser_ReturnsFailure()
    {
        // Arrange
        var command = new ResetPasswordCommand(new ResetPasswordRequest("test@example.com", "reset-token-123", "NewPass123!", "NewPass123!"));
        var user = CreateUser();
        var token = CreateValidToken();
        token.UserId = "another-user";

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("reset-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
