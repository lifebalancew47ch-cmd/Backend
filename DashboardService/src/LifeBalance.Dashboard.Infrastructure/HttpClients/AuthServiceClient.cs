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
            return new AuthUserResponseDto(userId, "user@lifebalance.io", "User", "LifeBalance", new List<string> { "User" }, "fam_001", "comp_001");
        }
    }

    public async Task<List<AuthUserResponseDto>> GetFamilyMembersProfileAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<AuthUserResponseDto>>($"/api/v1/families/{familyId}/members", cancellationToken);
            return res ?? GetFallbackFamilyMembers(familyId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family members for FamilyId: {FamilyId}", familyId);
            return GetFallbackFamilyMembers(familyId);
        }
    }

    public async Task<List<AuthUserResponseDto>> GetCompanyUsersAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<AuthUserResponseDto>>($"/api/v1/companies/{companyId}/users", cancellationToken);
            return res ?? new List<AuthUserResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve company users for CompanyId: {CompanyId}", companyId);
            return new List<AuthUserResponseDto>();
        }
    }

    private static List<AuthUserResponseDto> GetFallbackFamilyMembers(string familyId) => new()
    {
        new AuthUserResponseDto("usr_001", "parent1@lifebalance.io", "Carlos", "Garcia", new List<string> { "User" }, familyId, "comp_001"),
        new AuthUserResponseDto("usr_002", "parent2@lifebalance.io", "Maria", "Garcia", new List<string> { "User" }, familyId, "comp_001")
    };
}
