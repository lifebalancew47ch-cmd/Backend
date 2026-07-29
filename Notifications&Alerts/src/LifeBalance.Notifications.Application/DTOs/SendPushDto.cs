using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class SendPushDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public string? Payload { get; set; }

    public List<string> DeviceTokens { get; set; } = new();

    public DevicePlatform? Platform { get; set; }
}
