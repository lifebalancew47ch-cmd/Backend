using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class SendNotificationDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Body { get; set; } = string.Empty;
    
    public string? Payload { get; set; }
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required]
    public NotificationChannel Channel { get; set; }
}
