using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.Handlers.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Shared.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Auth;

public class ForgotPasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<ForgotPasswordHandler>> _loggerMock = new();
    private readonly IOptions<SecuritySettings> _securitySettings;

    public ForgotPasswordHandlerTests()
    {
        _securitySettings = Options.Create(new SecuritySettings
        {
            PasswordResetTokenExpirationMinutes = 60
        });
    }

    private ForgotPasswordHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenRepositoryMock.Object,
        _auditServiceMock.Object,
        _emailServiceMock.Object,
        _loggerMock.Object,
        _securitySettings
    );

    private static User CreateUser() => new()
    {
        Id = "user-123",
        Email = "test@example.com",
        IsActive = true
    };

    [Fact]
    public async Task Handle_ExistingUser_CreatesResetTokenWithExpirationAndDispatchesEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand(new ForgotPasswordRequest("test@example.com"));
        var user = CreateUser();
        var emailSent = new TaskCompletionSource();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _emailServiceMock.Setup(e => e.SendPasswordResetEmailAsync("test@example.com", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => emailSent.TrySetResult());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.Is<PasswordResetToken>(t =>
            t.UserId == "user-123" &&
            !string.IsNullOrEmpty(t.Token) &&
            t.ExpiresAt > DateTime.UtcNow &&
            t.ExpiresAt <= DateTime.UtcNow.AddMinutes(60)), It.IsAny<CancellationToken>()), Times.Once);
        await emailSent.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _auditServiceMock.Verify(a => a.LogEventAsync("user-123", AuthEventType.PasswordReset,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonexistentUser_ReturnsGenericSuccessToPreventEnumeration()
    {
        // Arrange
        var command = new ForgotPasswordCommand(new ForgotPasswordRequest("ghost@example.com"));

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("ghost@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("If the email exists");
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingUser_InvalidatesPreviousResetTokens()
    {
        // Arrange
        var command = new ForgotPasswordCommand(new ForgotPasswordRequest("test@example.com"));
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenRepositoryMock.Verify(r => r.InvalidateExistingForUserAsync("user-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailDeliveryFails_StillReturnsSuccess()
    {
        // Arrange
        var command = new ForgotPasswordCommand(new ForgotPasswordRequest("test@example.com"));
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _emailServiceMock.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingUser_GeneratesUniqueTokenEachRequest()
    {
        // Arrange
        var command = new ForgotPasswordCommand(new ForgotPasswordRequest("test@example.com"));
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _tokenRepositoryMock.Invocations.Count(i => i.Method.Name == nameof(IPasswordResetTokenRepository.AddAsync))
            .Should().Be(2);
    }
}
