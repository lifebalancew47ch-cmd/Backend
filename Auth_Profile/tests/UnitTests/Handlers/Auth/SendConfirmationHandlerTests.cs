using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.Handlers.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Shared.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace UnitTests.Handlers.Auth;

public class SendConfirmationHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IEmailConfirmationTokenRepository> _tokenRepositoryMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<SendConfirmationHandler>> _loggerMock = new();
    private readonly IOptions<SecuritySettings> _securitySettings;

    public SendConfirmationHandlerTests()
    {
        _securitySettings = Options.Create(new SecuritySettings
        {
            EmailConfirmationTokenExpirationHours = 24
        });
    }

    private SendConfirmationHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenRepositoryMock.Object,
        _auditServiceMock.Object,
        _emailServiceMock.Object,
        _loggerMock.Object,
        _securitySettings
    );

    private static User CreateUnconfirmedUser() => new()
    {
        Id = "user-123",
        Email = "test@example.com",
        IsEmailConfirmed = false
    };

    [Fact]
    public async Task Handle_UnconfirmedUser_CreatesNewTokenWithCorrectTtlAndDispatchesEmail()
    {
        // Arrange
        var command = new SendConfirmationCommand(new SendConfirmationRequest("test@example.com"));
        var user = CreateUnconfirmedUser();
        var emailSent = new TaskCompletionSource();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _emailServiceMock.Setup(e => e.SendEmailConfirmationEmailAsync("test@example.com", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => emailSent.TrySetResult());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.Is<EmailConfirmationToken>(t =>
            t.UserId == "user-123" &&
            t.Email == "test@example.com" &&
            !string.IsNullOrEmpty(t.Token) &&
            t.ExpiresAt > DateTime.UtcNow.AddHours(23) &&
            t.ExpiresAt <= DateTime.UtcNow.AddHours(24)), It.IsAny<CancellationToken>()), Times.Once);
        await emailSent.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Handle_NonexistentUser_ReturnsGenericSuccessToPreventEnumeration()
    {
        // Arrange
        var command = new SendConfirmationCommand(new SendConfirmationRequest("ghost@example.com"));

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("ghost@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("If the email exists");
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailConfirmationToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedUser_ReturnsGenericSuccessWithoutNewToken()
    {
        // Arrange
        var command = new SendConfirmationCommand(new SendConfirmationRequest("test@example.com"));
        var user = CreateUnconfirmedUser();
        user.IsEmailConfirmed = true;

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("If the email exists");
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailConfirmationToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnconfirmedUser_InvalidatesPreviousConfirmationTokens()
    {
        // Arrange
        var command = new SendConfirmationCommand(new SendConfirmationRequest("test@example.com"));
        var user = CreateUnconfirmedUser();

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
    public async Task Handle_EmailDeliveryFails_StillReturnsSuccessAndKeepsToken()
    {
        // Arrange
        var command = new SendConfirmationCommand(new SendConfirmationRequest("test@example.com"));
        var user = CreateUnconfirmedUser();

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _emailServiceMock.Setup(e => e.SendEmailConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailConfirmationToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
