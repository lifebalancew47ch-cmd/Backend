namespace LifeBalance.Notifications.Application.Interfaces;

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; }
    public bool IsActive { get; set; }
}

public interface IAuthServiceClient
{
    Task<UserInfo?> GetUserAsync(string userId);
    Task<string?> GetEmailAsync(string userId);
    Task<bool> GetPushNotificationsEnabledAsync(string userId);
}
