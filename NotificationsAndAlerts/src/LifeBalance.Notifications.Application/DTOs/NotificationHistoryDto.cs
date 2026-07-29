using LifeBalance.Notifications.Domain.Enums;

namespace LifeBalance.Notifications.Application.DTOs;

public class NotificationHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}