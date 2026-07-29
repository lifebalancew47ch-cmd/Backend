namespace LifeBalance.Notifications.Application.DTOs;

public class NotificationPreferenceDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool ReceivePush { get; set; }
    public bool ReceiveWearOS { get; set; }
    public bool ReceiveEmail { get; set; }
    public bool ReceiveSedentaryAlerts { get; set; }
    public bool ReceiveMarketing { get; set; }
    public DateTime UpdatedAt { get; set; }
}