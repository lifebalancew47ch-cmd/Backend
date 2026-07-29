using LifeBalance.Notifications.Domain.Enums;

namespace LifeBalance.Notifications.Application.DTOs;

public class NotificationHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
    public string? FamilyId { get; set; }
    public string? DepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public bool IsRead { get; set; }
    public bool IsArchived { get; set; }
    public bool IsFavorite { get; set; }
    public long? DeliveryTimeMs { get; set; }
    public int Attempts { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Provider { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
