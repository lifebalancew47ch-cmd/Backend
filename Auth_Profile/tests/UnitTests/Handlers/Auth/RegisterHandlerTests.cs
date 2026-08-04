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

public class RegisterHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<IEmailConfirmationTokenRepository> _emailTokenRepositoryMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IOrganizationService> _organizationServiceMock = new();
    private readonly Mock<ILogger<RegisterHandler>> _loggerMock = new();
    private readonly IOptions<SecuritySettings> _securitySettings;

    public RegisterHandlerTests()
    {
        _securitySettings = Options.Create(new SecuritySettings
        {
            EmailConfirmationTokenExpirationHours = 24
        });
        _organizationServiceMock
            .Setup(o => o.ProvisionMembershipAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantContextResult("tenant-1", "org-1"));
    }

    private RegisterHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _roleRepositoryMock.Object,
        _passwordServiceMock.Object,
        _auditServiceMock.Object,
        _emailTokenRepositoryMock.Object,
        _emailServiceMock.Object,
        _organizationServiceMock.Object,
        new AutoMapper.MapperConfiguration(cfg => { }).CreateMapper(),
        _loggerMock.Object,
        _securitySettings
    );

    private static RegisterRequest CreateRequest() =>
        new("Test@Example.com", "testuser", "Password123!", "Password123!", "Test", "User");

    [Fact]
    public async Task Handle_ValidRegistration_CreatesInactiveUserAndConfirmationToken()
    {
        // Arrange
        var request = CreateRequest();
        var command = new RegisterCommand(request);
        var defaultRole = new Role { Id = "role-user", Name = "User" };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepositoryMock.Setup(r => r.GetByNameAsync("User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultRole);
        _passwordServiceMock.Setup(p => p.HashPassword("Password123!"))
            .Returns("hashed_password");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("test@example.com");
        result.Data.RequiresEmailConfirmation.Should().BeTrue();

        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "test@example.com" &&
            u.IsEmailConfirmed == false &&
            u.IsActive &&
            u.RoleIds.SequenceEqual(new List<string> { "role-user" })),
            It.IsAny<CancellationToken>()), Times.Once);

        _emailTokenRepositoryMock.Verify(r => r.AddAsync(It.Is<EmailConfirmationToken>(t =>
            !string.IsNullOrEmpty(t.Token) && t.Email == "test@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditServiceMock.Verify(a => a.LogEventAsync(It.IsAny<string>(), AuthEventType.Register,
            It.IsAny<string>(), null, null, null, null, null, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRegistration_ProvisionsMembershipForNewUser()
    {
        // Arrange
        var command = new RegisterCommand(CreateRequest());
        var defaultRole = new Role { Id = "role-user", Name = "User" };

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultRole);
        _passwordServiceMock.Setup(p => p.HashPassword("Password123!"))
            .Returns("hashed_password");

        User? createdUser = null;
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        createdUser.Should().NotBeNull();
        _organizationServiceMock.Verify(o => o.ProvisionMembershipAsync(createdUser!.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailureAndDoesNotCreateUser()
    {
        // Arrange
        var command = new RegisterCommand(CreateRequest());

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync("Test@Example.com", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already registered");
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailConfirmationToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ReturnsFailureAndDoesNotCreateUser()
    {
        // Arrange
        var command = new RegisterCommand(CreateRequest());

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync("Test@Example.com", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync("testuser", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Username is already taken");
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingDefaultRole_RegistersUserWithoutRoles()
    {
        // Arrange
        var command = new RegisterCommand(CreateRequest());

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.RoleIds.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRegistration_DispatchesConfirmationEmail()
    {
        // Arrange
        var command = new RegisterCommand(CreateRequest());
        var emailSent = new TaskCompletionSource();

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);
        _emailServiceMock.Setup(e => e.SendEmailConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => emailSent.TrySetResult());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await emailSent.Task.WaitAsync(TimeSpan.FromSeconds(2));
        result.Success.Should().BeTrue();
    }
}
