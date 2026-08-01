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
using System.Security.Claims;
using Xunit;

namespace UnitTests.Handlers.Auth;

public class RefreshTokenHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IAuditService> _auditServiceMock = new();
    private readonly Mock<ILogger<RefreshTokenHandler>> _loggerMock = new();
    private readonly IOptions<JwtSettings> _jwtSettings;

    public RefreshTokenHandlerTests()
    {
        _jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "secret_key_for_testing_32_characters!",
            RefreshTokenExpirationDays = 7
        });
    }

    private RefreshTokenHandler CreateHandler() => new(
        _refreshTokenRepositoryMock.Object,
        _userRepositoryMock.Object,
        _roleRepositoryMock.Object,
        _jwtServiceMock.Object,
        _auditServiceMock.Object,
        _loggerMock.Object,
        _jwtSettings
    );

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest("expired_access_token", "valid_refresh_token");
        var command = new RefreshTokenCommand(request);

        var claims = new List<Claim> { new("jti", "jwt-id-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var existingRefreshToken = new RefreshToken
        {
            Id = "rt-1",
            Token = "valid_refresh_token",
            JwtId = "jwt-id-123",
            UserId = "user-123",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var user = new User { Id = "user-123", Email = "test@example.com", IsActive = true };

        _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns(principal);
        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("valid_refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRefreshToken);
        _userRepositoryMock.Setup(u => u.GetByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _roleRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("new_access_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("new_refresh_token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new_access_token");
        result.Data.RefreshToken.Should().Be("new_refresh_token");
        existingRefreshToken.IsActive.Should().BeFalse();
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ReturnsFailResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest("expired_access_token", "expired_refresh_token");
        var command = new RefreshTokenCommand(request);

        var claims = new List<Claim> { new("jti", "jwt-id-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var expiredRefreshToken = new RefreshToken
        {
            Id = "rt-1",
            Token = "expired_refresh_token",
            JwtId = "jwt-id-123",
            UserId = "user-123",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired
        };

        _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns(principal);
        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("expired_refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredRefreshToken);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no longer valid");
    }

    [Fact]
    public async Task Handle_JwtIdMismatch_RevokesAllUserSessionsAndReturnsTokenReuseError()
    {
        // Arrange
        var request = new RefreshTokenRequest("stolen_access_token", "valid_refresh_token");
        var command = new RefreshTokenCommand(request);

        var claims = new List<Claim> { new("jti", "different-jwt-id") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var refreshToken = new RefreshToken
        {
            Id = "rt-1",
            Token = "valid_refresh_token",
            JwtId = "original-jwt-id",
            UserId = "user-123",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken("stolen_access_token"))
            .Returns(principal);
        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("valid_refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Token reuse detected");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllByUserIdAsync("user-123", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
