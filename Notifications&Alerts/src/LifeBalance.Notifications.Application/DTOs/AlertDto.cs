using LifeBalance.Notifications.Domain.Enums;

namespace LifeBalance.Notifications.Application.DTOs;

public class AlertDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public AlertPriority Priority { get; set; }
    public bool IsRead { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
