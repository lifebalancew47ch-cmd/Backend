namespace LifeBalance.Notifications.Application.Interfaces;

public class PushResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string Provider { get; set; } = string.Empty;
}

public interface IPushNotificationProvider
{
    Task<PushResult> SendToDeviceAsync(string deviceToken, string title, string body, string? payload);
    Task<List<PushResult>> SendToDevicesAsync(List<string> deviceTokens, string title, string body, string? payload);
}
