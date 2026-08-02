using Auth.Infrastructure.Services;
using Auth.Shared.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace UnitTests.Services;

public class JwtServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "super_secret_test_key_32_characters_long!",
            Issuer = "LifeBalanceTestIssuer",
            Audience = "LifeBalanceTestAudience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        _jwtService = new JwtService(Options.Create(_jwtSettings));
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidTokenWithClaims()
    {
        // Arrange
        var inputClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-999"),
            new(ClaimTypes.Email, "jwt@example.com"),
            new(ClaimTypes.Role, "Admin")
        };

        // Act
        var tokenString = _jwtService.GenerateAccessToken(inputClaims);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
        // Auth emite claims con nombres cortos estándar de JWT (sub/email/role).
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "user-999");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "jwt@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceUrlSafeToken()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
        token1.Should().NotContain("+");
        token1.Should().NotContain("/");
        token1.Should().NotEndWith("=");
    }

    [Fact]
    public void GetJwtId_ShouldExtractJtiFromToken()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.Email, "test@example.com") };
        var token = _jwtService.GenerateAccessToken(claims);

        // Act
        var jti = _jwtService.GetJwtId(token);

        // Assert
        jti.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(jti, out _).Should().BeTrue();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldExtractClaimsWithoutValidatingLifetime()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-expired"),
            new(ClaimTypes.Email, "expired@example.com")
        };
        var token = _jwtService.GenerateAccessToken(claims);

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user-expired");
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be("expired@example.com");
    }
}
