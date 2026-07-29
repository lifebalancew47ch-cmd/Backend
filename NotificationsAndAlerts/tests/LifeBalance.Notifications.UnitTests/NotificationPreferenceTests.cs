using LifeBalance.Notifications.Domain.Entities;
using Xunit;

namespace LifeBalance.Notifications.UnitTests;

public class NotificationPreferenceTests
{
    [Fact]
    public void NewNotificationPreference_ShouldHaveAllChannelsEnabledByDefault()
    {
        // Arrange & Act
        var pref = new NotificationPreference
        {
            UserId = "user_123"
        };

        // Assert
        Assert.True(pref.ReceivePush);
        Assert.True(pref.ReceiveWearOS);
        Assert.True(pref.ReceiveEmail);
        Assert.True(pref.ReceiveSedentaryAlerts);
        Assert.True(pref.ReceiveMarketing);
    }

    [Fact]
    public void NotificationPreference_WhenPushDisabled_ShouldReflectChange()
    {
        // Arrange
        var pref = new NotificationPreference { UserId = "user_123" };

        // Act
        pref.ReceivePush = false;

        // Assert
        Assert.False(pref.ReceivePush);
        Assert.True(pref.ReceiveEmail); // other channels unaffected
    }

    [Fact]
    public void NotificationPreference_WhenAllDisabled_ShouldReflectAllFalse()
    {
        // Arrange
        var pref = new NotificationPreference { UserId = "user_123" };

        // Act
        pref.ReceivePush = false;
        pref.ReceiveWearOS = false;
        pref.ReceiveEmail = false;
        pref.ReceiveSedentaryAlerts = false;
        pref.ReceiveMarketing = false;

        // Assert
        Assert.False(pref.ReceivePush);
        Assert.False(pref.ReceiveWearOS);
        Assert.False(pref.ReceiveEmail);
        Assert.False(pref.ReceiveSedentaryAlerts);
        Assert.False(pref.ReceiveMarketing);
    }

    [Fact]
    public void NotificationPreference_UpdatedAt_ShouldBeInitializedOnCreation()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var pref = new NotificationPreference { UserId = "user_123" };

        // Assert
        Assert.True(pref.UpdatedAt >= before);
    }
}
