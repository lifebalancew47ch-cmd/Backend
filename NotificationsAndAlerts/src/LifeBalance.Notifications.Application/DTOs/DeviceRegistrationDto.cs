using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class DeviceRegistrationDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string DeviceToken { get; set; } = string.Empty;

    public DevicePlatform Platform { get; set; }
}
