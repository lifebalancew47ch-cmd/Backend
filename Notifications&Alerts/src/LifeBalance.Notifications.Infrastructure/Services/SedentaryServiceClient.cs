using System.Net.Http.Json;
using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class SedentaryServiceClient : ISedentaryServiceClient
{
    private readonly HttpClient _httpClient;

    public SedentaryServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:SedentaryService"] ?? "http://localhost:5003");
    }

    public async Task ProcessActiveBreakReminderAsync(SedentaryAlertRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/active-breaks", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ProcessGoalReminderAsync(SedentaryAlertRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/goals/reminders", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ProcessSedentaryScoreAlertAsync(SedentaryAlertRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/sedentary-score", request);
        response.EnsureSuccessStatusCode();
    }
}
