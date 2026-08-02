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

public class ChangePasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILogger<ChangePasswordHandler>> _loggerMock = new();

    private ChangePasswordHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _passwordServiceMock.Object,
        _auditServiceMock.Object,
        _loggerMock.Object
    );

    private static User CreateUser() => new()
    {
        Id = "user-123",
        Email = "test@example.com",
        PasswordHash = "current_hash",
        LastPasswordChangeAt = DateTime.UtcNow.AddDays(-30)
    };

    [Fact]
    public async Task Handle_CorrectCurrentPassword_HashesNewPasswordAndUpdatesTimestamp()
    {
        // Arrange
        var command = new ChangePasswordCommand(new ChangePasswordRequest("CurrentPass123!", "NewPass456!", "NewPass456!"), "user-123");
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("CurrentPass123!", "current_hash"))
            .Returns(true);
        _passwordServiceMock.Setup(p => p.HashPassword("NewPass456!"))
            .Returns("new_hash");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        user.PasswordHash.Should().Be("new_hash");
        user.LastPasswordChangeAt.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.PasswordChange,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentUser_ReturnsFailure()
    {
        // Arrange
        var command = new ChangePasswordCommand(new ChangePasswordRequest("CurrentPass123!", "NewPass456!", "NewPass456!"), "ghost-user");

        _userRepositoryMock.Setup(r => r.GetByIdAsync("ghost-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("User not found");
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsFailureAndDoesNotUpdate()
    {
        // Arrange
        var command = new ChangePasswordCommand(new ChangePasswordRequest("WrongPass123!", "NewPass456!", "NewPass456!"), "user-123");
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("WrongPass123!", "current_hash"))
            .Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Current password is incorrect");
        user.PasswordHash.Should().Be("current_hash");
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditServiceMock.Verify(a => a.LogEventAsync(It.IsAny<string?>(), It.IsAny<AuthEventType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_VerifiesAgainstStoredHash()
    {
        // Arrange
        var command = new ChangePasswordCommand(new ChangePasswordRequest("CurrentPass123!", "NewPass456!", "NewPass456!"), "user-123");
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("CurrentPass123!", "current_hash"))
            .Returns(true);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordServiceMock.Verify(p => p.VerifyPassword("CurrentPass123!", "current_hash"), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword("NewPass456!"), Times.Once);
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_PersistsUpdatedUser()
    {
        // Arrange
        var command = new ChangePasswordCommand(new ChangePasswordRequest("CurrentPass123!", "NewPass456!", "NewPass456!"), "user-123");
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.Id == "user-123" && u.PasswordHash == It.IsAny<string>()),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
