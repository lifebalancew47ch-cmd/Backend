using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class BroadcastPushDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public string? Payload { get; set; }

    public List<string>? UserIds { get; set; }

    public string? OrganizationId { get; set; }

    public string? FamilyId { get; set; }

    public string? DepartmentId { get; set; }

    public DevicePlatform? Platform { get; set; }
}
