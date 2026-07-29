using Auth.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public class UserLockoutTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void IncrementFailedLoginAttempts_MultipleTimes_ShouldAccumulate(int times)
    {
        // Arrange
        var user = new User();

        // Act
        for (int i = 0; i < times; i++)
            user.IncrementFailedLoginAttempts();

        // Assert
        user.FailedLoginAttempts.Should().Be(times);
    }

    [Fact]
    public void LockOut_ShouldSetLockoutEndInFuture()
    {
        // Arrange
        var user = new User();
        var before = DateTime.UtcNow;

        // Act
        user.LockOut(TimeSpan.FromMinutes(15));

        // Assert
        user.LockoutEnd.Should().BeAfter(before);
        user.IsLockedOut.Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutEndIsInPast_ShouldBeFalse()
    {
        // Arrange
        var user = new User
        {
            LockoutEnd = DateTime.UtcNow.AddSeconds(-1)
        };

        // Act & Assert
        user.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutEndIsNull_ShouldBeFalse()
    {
        // Arrange
        var user = new User { LockoutEnd = null };

        // Act & Assert
        user.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void ResetFailedLoginAttempts_AfterLockout_ShouldClearAllState()
    {
        // Arrange
        var user = new User();
        user.IncrementFailedLoginAttempts();
        user.IncrementFailedLoginAttempts();
        user.LockOut(TimeSpan.FromMinutes(15));

        // Act
        user.ResetFailedLoginAttempts();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void NewUser_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.IsLockedOut.Should().BeFalse();
        user.LockoutEnd.Should().BeNull();
        user.IsActive.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeFalse();
    }
}
