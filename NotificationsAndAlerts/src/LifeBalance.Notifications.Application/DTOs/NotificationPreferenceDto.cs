namespace LifeBalance.Notifications.Application.DTOs;

public class NotificationPreferenceDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool ReceivePush { get; set; }
    public bool ReceiveEmail { get; set; }
    public bool ReceiveSms { get; set; }
    public bool ReceiveWearOS { get; set; }
    public bool ReceiveCriticalAlerts { get; set; }
    public bool ReceiveReminders { get; set; }
    public bool ReceiveGoals { get; set; }
    public bool ReceiveGamification { get; set; }
    public bool ReceiveOrganizational { get; set; }
    public string? AllowedStartTime { get; set; }
    public string? AllowedEndTime { get; set; }
    public bool QuietModeEnabled { get; set; }
    public string? QuietModeStart { get; set; }
    public string? QuietModeEnd { get; set; }
    public string? Frequency { get; set; }
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public DateTime UpdatedAt { get; set; }
}
