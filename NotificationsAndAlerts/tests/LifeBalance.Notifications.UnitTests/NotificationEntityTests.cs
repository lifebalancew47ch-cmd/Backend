using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using Xunit;

namespace LifeBalance.Notifications.UnitTests;

public class NotificationEntityTests
{
    [Fact]
    public void NewNotification_ShouldHaveDefaultPendingStatus()
    {
        // Arrange & Act
        var notification = new Notification
        {
            UserId = "user_123",
            Title = "Test Notification",
            Body = "This is a test.",
            Type = NotificationType.Information,
            Channel = NotificationChannel.Push,
            Status = NotificationStatus.Pending
        };

        // Assert
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Null(notification.SentAt);
    }

    [Fact]
    public void Notification_WhenSent_ShouldHaveSentAtSet()
    {
        // Arrange
        var notification = new Notification
        {
            UserId = "user_123",
            Title = "Goal Reached!",
            Body = "You completed your daily steps goal.",
            Type = NotificationType.GoalCompleted,
            Channel = NotificationChannel.Push,
            Status = NotificationStatus.Pending
        };

        // Act
        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTime.UtcNow;

        // Assert
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentAt);
    }

    [Fact]
    public void Notification_WithPayload_ShouldStorePayloadCorrectly()
    {
        // Arrange
        var payload = "{\"goalId\": \"goal_abc\", \"value\": 10000}";

        // Act
        var notification = new Notification
        {
            UserId = "user_123",
            Title = "Achievement!",
            Body = "You unlocked a badge.",
            Type = NotificationType.AchievementUnlocked,
            Channel = NotificationChannel.Push,
            Status = NotificationStatus.Pending,
            Payload = payload
        };

        // Assert
        Assert.Equal(payload, notification.Payload);
    }

    [Fact]
    public void Notification_WithoutPayload_PayloadShouldBeNull()
    {
        // Arrange & Act
        var notification = new Notification
        {
            UserId = "user_123",
            Title = "Reminder",
            Body = "Time for a break!",
            Type = NotificationType.ActiveBreakReminder,
            Channel = NotificationChannel.Push,
            Status = NotificationStatus.Pending
        };

        // Assert
        Assert.Null(notification.Payload);
    }

    [Theory]
    [InlineData(NotificationType.SedentaryAlert)]
    [InlineData(NotificationType.GoalCompleted)]
    [InlineData(NotificationType.AchievementUnlocked)]
    [InlineData(NotificationType.OrganizationInvitation)]
    [InlineData(NotificationType.Reminder)]
    public void Notification_ShouldAcceptAllNotificationTypes(NotificationType type)
    {
        // Arrange & Act
        var notification = new Notification
        {
            UserId = "user_123",
            Title = "Test",
            Body = "Body",
            Type = type,
            Channel = NotificationChannel.Push,
            Status = NotificationStatus.Pending
        };

        // Assert
        Assert.Equal(type, notification.Type);
    }
}
