using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class BroadcastPushDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Payload { get; set; }

    public List<string>? UserIds { get; set; }

    public string? OrganizationId { get; set; }

    public string? FamilyId { get; set; }

    public string? DepartmentId { get; set; }

    public DevicePlatform? Platform { get; set; }
}
