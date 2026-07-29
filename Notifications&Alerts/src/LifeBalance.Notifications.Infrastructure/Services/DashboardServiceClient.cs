using System.Net.Http.Json;
using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class DashboardServiceClient : IDashboardServiceClient
{
    private readonly HttpClient _httpClient;

    public DashboardServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:DashboardService"] ?? "http://localhost:5006");
    }

    public async Task PushNotificationHistoryAsync(object historyData)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/dashboard/notifications", historyData);
        response.EnsureSuccessStatusCode();
    }
}
