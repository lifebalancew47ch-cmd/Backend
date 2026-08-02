using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class SedentaryEngineServiceClient : ISedentaryEngineServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SedentaryEngineServiceClient> _logger;

    public SedentaryEngineServiceClient(HttpClient httpClient, ILogger<SedentaryEngineServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SedentaryActivityResponseDto?> GetUserActivityAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SedentaryActivityResponseDto>($"/api/v1/sedentary/user/{userId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve sedentary activity for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<CompanyAdherenceResponseDto?> GetCompanyAdherenceAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CompanyAdherenceResponseDto>($"/api/v1/sedentary/company/{companyId}/adherence", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve company adherence for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }
}
