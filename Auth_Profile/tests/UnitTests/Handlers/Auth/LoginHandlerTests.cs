using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.DTOs.Profile;
using Auth.Application.Handlers.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Shared.Common;
using Auth.Shared.Configurations;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace UnitTests.Handlers.Auth;

public class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILoginHistoryRepository> _loginHistoryRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<LoginHandler>> _loggerMock = new();
    private readonly IOptions<SecuritySettings> _securitySettings;
    private readonly IOptions<JwtSettings> _jwtSettings;

    public LoginHandlerTests()
    {
        _securitySettings = Options.Create(new SecuritySettings
        {
            MaxFailedLoginAttempts = 3,
            LockoutDurationMinutes = 15
        });

        _jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "super_secret_key_for_unit_tests_only_32_chars!",
            Issuer = "LifeBalance",
            Audience = "LifeBalance",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        });
    }

    private LoginHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _roleRepositoryMock.Object,
        _refreshTokenRepositoryMock.Object,
        _jwtServiceMock.Object,
        _passwordServiceMock.Object,
        _auditServiceMock.Object,
        _loginHistoryRepositoryMock.Object,
        _mapperMock.Object,
        _loggerMock.Object,
        _securitySettings,
        _jwtSettings
    );

    private static UserProfileDto CreateDummyProfile() =>
        new("user-123", "test@example.com", "testuser", "Test", "User", null, null, true, true, DateTime.UtcNow, null);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var command = new LoginCommand(request);
        var user = new User
        {
            Id = "user-123",
            Email = "test@example.com",
            PasswordHash = "hashed_pass",
            IsActive = true,
            RoleIds = new List<string> { "role-1" }
        };
        var role = new Role { Id = "role-1", Name = "Admin", NormalizedName = "ADMIN" };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("Password123!", "hashed_pass"))
            .Returns(true);
        _roleRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { role });
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("fake_access_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("fake_refresh_token");
        _jwtServiceMock.Setup(j => j.GetJwtId("fake_access_token"))
            .Returns("jwt-id-123");
        _jwtServiceMock.Setup(j => j.GetAccessTokenExpiration())
            .Returns(DateTime.UtcNow.AddMinutes(30));
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateDummyProfile());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("fake_access_token");
        result.Data.RefreshToken.Should().Be("fake_refresh_token");
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _loginHistoryRepositoryMock.Verify(l => l.AddAsync(It.Is<LoginHistory>(h => h.Success), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_Returns401()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "Password123!");
        var command = new LoginCommand(request);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Message.Should().Contain("Invalid email or password");
        _loginHistoryRepositoryMock.Verify(l => l.AddAsync(It.Is<LoginHistory>(h => !h.Success), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidPassword_IncrementsFailedAttemptsAndReturns401()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");
        var command = new LoginCommand(request);
        var user = new User
        {
            Id = "user-123",
            Email = "test@example.com",
            PasswordHash = "hashed_pass",
            IsActive = true,
            FailedLoginAttempts = 0
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("WrongPassword", "hashed_pass"))
            .Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        user.FailedLoginAttempts.Should().Be(1);
        _userRepositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExceedsMaxFailedAttempts_LocksAccount()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");
        var command = new LoginCommand(request);
        var user = new User
        {
            Id = "user-123",
            Email = "test@example.com",
            PasswordHash = "hashed_pass",
            IsActive = true,
            FailedLoginAttempts = 2 // Max is 3
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("WrongPassword", "hashed_pass"))
            .Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        user.IsLockedOut.Should().BeTrue();
        _auditServiceMock.Verify(a => a.LogEventAsync(
            user.Id,
            AuthEventType.AccountLockout,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LockedOutAccount_Returns401()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var command = new LoginCommand(request);
        var user = new User
        {
            Id = "user-123",
            Email = "test@example.com",
            IsActive = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(10)
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Message.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_InactiveAccount_Returns401()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var command = new LoginCommand(request);
        var user = new User
        {
            Id = "user-123",
            Email = "test@example.com",
            IsActive = false
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Message.Should().Contain("inactive");
    }

    [Fact]
    public async Task Handle_UserWithNoRoles_AssignsDefaultUserRoleClaimAndSucceeds()
    {
        // Arrange
        var request = new LoginRequest("noroles@example.com", "Password123!");
        var command = new LoginCommand(request);
        var user = new User
        {
            Id = "user-noroles",
            Email = "noroles@example.com",
            PasswordHash = "hashed_pass",
            IsActive = true,
            RoleIds = new List<string>()
        };
        var defaultRole = new Role { Id = "role-user", Name = "User", NormalizedName = "USER" };

        IEnumerable<Claim>? issuedClaims = null;

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("noroles@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("Password123!", "hashed_pass"))
            .Returns(true);
        _roleRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());
        _roleRepositoryMock.Setup(r => r.GetByNameAsync("User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultRole);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Callback<IEnumerable<Claim>>(c => issuedClaims = c)
            .Returns("fake_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("fake_refresh");
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateDummyProfile());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        issuedClaims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "USER");
    }

    [Fact]
    public async Task Handle_UserWithNoRolesAndNoDefaultRoleInRepo_LoginSucceedsWithoutRoleClaim()
    {
        // Arrange
        var request = new LoginRequest("noroles2@example.com", "Password123!");
        var command = new LoginCommand(request);
        var user = new User
        {
            Id = "user-noroles2",
            Email = "noroles2@example.com",
            PasswordHash = "hashed_pass",
            IsActive = true,
            RoleIds = new List<string>()
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("noroles2@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword("Password123!", "hashed_pass"))
            .Returns(true);
        _roleRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());
        _roleRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("fake_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("fake_refresh");
        _mapperMock.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
            .Returns(CreateDummyProfile());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }
}
