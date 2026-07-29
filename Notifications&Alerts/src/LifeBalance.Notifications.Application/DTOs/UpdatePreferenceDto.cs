namespace LifeBalance.Notifications.Application.DTOs;

public class UpdatePreferenceDto
{
    public bool? ReceivePush { get; set; }
    public bool? ReceiveWearOS { get; set; }
    public bool? ReceiveEmail { get; set; }
    public bool? ReceiveSedentaryAlerts { get; set; }
    public bool? ReceiveMarketing { get; set; }
}