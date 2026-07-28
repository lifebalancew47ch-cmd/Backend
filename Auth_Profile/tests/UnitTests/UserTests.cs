using Auth.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public class UserTests
{
    [Fact]
    public void IncrementFailedLoginAttempts_ShouldIncreaseCount()
    {
        // Arrange
        var user = new User();

        // Act
        user.IncrementFailedLoginAttempts();

        // Assert
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public void LockOut_ShouldSetLockoutEndDate()
    {
        // Arrange
        var user = new User();
        var duration = TimeSpan.FromMinutes(15);

        // Act
        user.LockOut(duration);

        // Assert
        user.IsLockedOut.Should().BeTrue();
        user.LockoutEnd.Should().NotBeNull();
    }

    [Fact]
    public void ResetFailedLoginAttempts_ShouldClearCountAndLockout()
    {
        // Arrange
        var user = new User();
        user.IncrementFailedLoginAttempts();
        user.LockOut(TimeSpan.FromMinutes(15));

        // Act
        user.ResetFailedLoginAttempts();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.IsLockedOut.Should().BeFalse();
    }
}
