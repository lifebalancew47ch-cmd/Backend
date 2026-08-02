using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class SendPushDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Payload { get; set; }

    public List<string> DeviceTokens { get; set; } = new();

    public DevicePlatform? Platform { get; set; }
}
