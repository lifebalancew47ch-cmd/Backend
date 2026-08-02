using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class CreateTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string BodyContent { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? HtmlContent { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    public NotificationChannel Channel { get; set; }

    public List<string> Variables { get; set; } = new();

    public bool IsGlobal { get; set; }
}
