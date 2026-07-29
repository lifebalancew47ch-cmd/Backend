namespace LifeBalance.Notifications.Application.Interfaces;

public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> DeviceTokens { get; set; } = new();
    public List<string> PushTokens { get; set; } = new();
}

public interface IAuthServiceClient
{
    Task<UserInfo?> GetUserAsync(string userId);
    Task<string?> GetEmailAsync(string userId);
    Task<List<string>> GetDeviceTokensAsync(string userId);
    Task<List<string>> GetPushTokensAsync(string userId);
}
