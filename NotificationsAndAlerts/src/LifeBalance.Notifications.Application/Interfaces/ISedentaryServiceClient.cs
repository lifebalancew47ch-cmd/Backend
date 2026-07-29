namespace LifeBalance.Notifications.Application.Interfaces;

public class SedentaryAlertRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int SedentaryScore { get; set; }
}

public interface ISedentaryServiceClient
{
    Task ProcessActiveBreakReminderAsync(SedentaryAlertRequest request);
    Task ProcessGoalReminderAsync(SedentaryAlertRequest request);
    Task ProcessSedentaryScoreAlertAsync(SedentaryAlertRequest request);
}
