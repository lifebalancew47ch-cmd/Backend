using System.Net.Http.Json;
using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        // Auth API runs on http://localhost:5200
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:AuthService"] ?? "http://localhost:5200");
        _logger = logger;
    }

    public async Task<UserInfo?> GetUserAsync(string userId)
    {
        try
        {
            // Auth API uses: GET /api/v1/profile/me (requires JWT, returns ApiResponse<UserProfileDto>)
            var response = await _httpClient.GetAsync($"/api/v1/profile/me");
            response.EnsureSuccessStatusCode();
            // Unwrap the ApiResponse wrapper
            var wrapper = await response.Content.ReadFromJsonAsync<AuthApiResponse<AuthUserProfileDto>>();
            if (wrapper?.Success == true && wrapper.Data != null)
            {
                return new UserInfo
                {
                    Id = wrapper.Data.Id,
                    Email = wrapper.Data.Email,
                    Username = wrapper.Data.Username,
                    FirstName = wrapper.Data.FirstName,
                    LastName = wrapper.Data.LastName,
                    IsEmailConfirmed = wrapper.Data.IsEmailConfirmed,
                    IsActive = wrapper.Data.IsActive
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user {UserId} from Auth API", userId);
            return null;
        }
    }

    public async Task<string?> GetEmailAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.Email;
    }

    public async Task<bool> GetPushNotificationsEnabledAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profile/preferences");
            response.EnsureSuccessStatusCode();
            var wrapper = await response.Content.ReadFromJsonAsync<AuthApiResponse<AuthUserPreferenceDto>>();
            return wrapper?.Data?.PushNotificationsEnabled ?? true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get push preferences for user {UserId}", userId);
            return true; // default to enabled
        }
    }
}

// Auth API response wrapper
internal class AuthApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

internal record AuthUserProfileDto(string Id, string Email, string Username, string FirstName, string LastName, string? PhoneNumber, string? AvatarUrl, bool IsEmailConfirmed, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);
internal record AuthUserPreferenceDto(string Theme, string Language, string Timezone, string UnitsSystem, bool NotificationsEnabled, bool EmailNotificationsEnabled, bool PushNotificationsEnabled, string ProfileVisibility, bool MarketingConsent, bool ActivitySharing);
