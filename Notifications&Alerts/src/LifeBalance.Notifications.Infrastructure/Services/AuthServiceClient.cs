using System.Net.Http.Json;
using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;

    public AuthServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:AuthService"] ?? "http://localhost:5001");
    }

    public async Task<UserInfo?> GetUserAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/users/{userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserInfo>();
    }

    public async Task<string?> GetEmailAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.Email;
    }

    public async Task<List<string>> GetDeviceTokensAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.DeviceTokens ?? new List<string>();
    }

    public async Task<List<string>> GetPushTokensAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user?.PushTokens ?? new List<string>();
    }
}
