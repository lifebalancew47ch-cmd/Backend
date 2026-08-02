using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthUserResponseDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AuthUserResponseDto>($"/api/v1/users/{userId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve user profile for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<List<AuthUserResponseDto>?> GetFamilyMembersProfileAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<AuthUserResponseDto>>($"/api/v1/families/{familyId}/members", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family members for FamilyId: {FamilyId}", familyId);
            return null;
        }
    }

    public async Task<List<AuthUserResponseDto>?> GetCompanyUsersAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<AuthUserResponseDto>>($"/api/v1/companies/{companyId}/users", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve company users for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }
}
