using LifeBalance.Notifications.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LifeBalance.Notifications.Application.DTOs;

public class ScheduleRequestDto
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

    public string? OrganizationId { get; set; }

    public string? FamilyId { get; set; }

    public string? DepartmentId { get; set; }

    [Required]
    public DateTime ScheduledFor { get; set; }

    public RecurrencePattern Recurrence { get; set; }

    public int? RepeatInterval { get; set; }
}
