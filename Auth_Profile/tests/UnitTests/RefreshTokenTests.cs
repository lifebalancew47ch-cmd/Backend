using Auth.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public class RefreshTokenTests
{
    [Fact]
    public void IsExpired_WhenExpiresAtIsInThePast_ShouldBeTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act & Assert
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInTheFuture_ShouldBeFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act & Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenRevokedAtIsNull_ShouldBeFalse()
    {
        // Arrange
        var token = new RefreshToken { RevokedAt = null };

        // Act & Assert
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenRevokedAtIsSet_ShouldBeTrue()
    {
        // Arrange
        var token = new RefreshToken { RevokedAt = DateTime.UtcNow };

        // Act & Assert
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActiveAndNotExpired_WhenActiveAndNotExpiredAndNotRevoked_ShouldBeTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Act & Assert
        token.IsActiveAndNotExpired.Should().BeTrue();
    }

    [Fact]
    public void IsActiveAndNotExpired_WhenExpired_ShouldBeFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1),
            RevokedAt = null
        };

        // Act & Assert
        token.IsActiveAndNotExpired.Should().BeFalse();
    }

    [Fact]
    public void IsActiveAndNotExpired_WhenRevoked_ShouldBeFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };

        // Act & Assert
        token.IsActiveAndNotExpired.Should().BeFalse();
    }

    [Fact]
    public void IsActiveAndNotExpired_WhenInactive_ShouldBeFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Act & Assert
        token.IsActiveAndNotExpired.Should().BeFalse();
    }
}
