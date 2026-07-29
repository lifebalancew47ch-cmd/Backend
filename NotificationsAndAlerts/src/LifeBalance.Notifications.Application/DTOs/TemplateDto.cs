using LifeBalance.Notifications.Domain.Enums;

namespace LifeBalance.Notifications.Application.DTOs;

public class TemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyContent { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public List<string> Variables { get; set; } = new();
    public int Version { get; set; }
    public bool IsGlobal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
