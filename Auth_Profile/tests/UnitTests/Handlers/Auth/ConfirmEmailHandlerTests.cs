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

public class ConfirmEmailHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IEmailConfirmationTokenRepository> _tokenRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILogger<ConfirmEmailHandler>> _loggerMock = new();

    private ConfirmEmailHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenRepositoryMock.Object,
        _auditServiceMock.Object,
        _loggerMock.Object
    );

    private static User CreateUser() => new()
    {
        Id = "user-123",
        Email = "test@example.com",
        IsEmailConfirmed = false
    };

    private static EmailConfirmationToken CreateValidToken() => new()
    {
        Id = "ect-1",
        UserId = "user-123",
        Token = "confirmation-token-123",
        Email = "test@example.com",
        ExpiresAt = DateTime.UtcNow.AddHours(12),
        IsConfirmed = false
    };

    [Fact]
    public async Task Handle_ValidToken_ConfirmsEmailAndMarksTokenConfirmed()
    {
        // Arrange
        var command = new ConfirmEmailCommand(new ConfirmEmailRequest("test@example.com", "confirmation-token-123"));
        var user = CreateUser();
        var token = CreateValidToken();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("confirmation-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeTrue();
        token.IsConfirmed.Should().BeTrue();
        token.ConfirmedAt.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepositoryMock.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.EmailConfirmation,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentUser_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmEmailCommand(new ConfirmEmailRequest("ghost@example.com", "any-token"));

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("ghost@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Invalid confirmation request");
        _tokenRepositoryMock.Verify(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmEmailCommand(new ConfirmEmailRequest("test@example.com", "confirmation-token-123"));
        var user = CreateUser();
        var token = CreateValidToken();
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("confirmation-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired confirmation token");
        user.IsEmailConfirmed.Should().BeFalse();
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReusedConfirmedToken_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmEmailCommand(new ConfirmEmailRequest("test@example.com", "confirmation-token-123"));
        var user = CreateUser();
        var token = CreateValidToken();
        token.IsConfirmed = true;
        token.ConfirmedAt = DateTime.UtcNow.AddDays(-1);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("confirmation-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired confirmation token");
    }

    [Fact]
    public async Task Handle_TokenBelongingToAnotherUser_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmEmailCommand(new ConfirmEmailRequest("test@example.com", "confirmation-token-123"));
        var user = CreateUser();
        var token = CreateValidToken();
        token.UserId = "another-user";

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenRepositoryMock.Setup(r => r.GetByTokenAsync("confirmation-token-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        user.IsEmailConfirmed.Should().BeFalse();
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
