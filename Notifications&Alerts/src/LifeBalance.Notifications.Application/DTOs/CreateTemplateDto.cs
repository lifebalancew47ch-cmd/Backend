using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class CreateTemplateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyContent { get; set; } = string.Empty;

    public string? HtmlContent { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    public NotificationChannel Channel { get; set; }

    public List<string> Variables { get; set; } = new();

    public bool IsGlobal { get; set; }
}
