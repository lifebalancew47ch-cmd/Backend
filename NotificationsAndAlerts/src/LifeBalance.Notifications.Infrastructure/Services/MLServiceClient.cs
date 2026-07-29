using System.Net.Http.Json;
using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class MLServiceClient : IMLServiceClient
{
    private readonly HttpClient _httpClient;
    public MLServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:MLService"] ?? "http://localhost:5006");
    }

    public async Task ProcessPredictiveAlertAsync(PredictAlertRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/predictive-alerts", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ProcessRecommendationAsync(PredictAlertRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/recommendations", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ProcessSedentaryRiskAsync(PredictAlertRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/sedentary-risk", request);
        response.EnsureSuccessStatusCode();
    }
}
