using System.Net.Http.Json;
using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class AdminServiceClient : IAdminServiceClient
{
    private readonly HttpClient _httpClient;
    public AdminServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:AdminService"] ?? "http://localhost:5005");
    }

    public async Task<List<GlobalTemplate>> GetGlobalTemplatesAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/templates/global");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GlobalTemplate>>() ?? new();
    }

    public async Task<AdminConfiguration?> GetConfigurationAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/configuration");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminConfiguration>();
    }
}
