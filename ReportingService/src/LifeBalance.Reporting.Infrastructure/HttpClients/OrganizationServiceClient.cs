using System.Net.Http.Json;
using LifeBalance.Reporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Reporting.Infrastructure.HttpClients;

/// <summary>
/// Implementation of <see cref="IOrganizationServiceClient"/> using a typed <see cref="HttpClient"/>.
/// Returns <c>null</c> on failure so callers fail closed (503).
/// </summary>
public sealed class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationServiceClient> _logger;

    /// <summary>Initializes a new instance of <see cref="OrganizationServiceClient"/>.</summary>
    public OrganizationServiceClient(HttpClient httpClient, ILogger<OrganizationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CompanyDto?> GetCompanyAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OrganizationApiResponse<CompanyDto>>(
                $"/api/v1/organizations/{companyId}", cancellationToken);
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve company for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<FamilyMembershipDto?> GetFamilyAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OrganizationApiResponse<FamilyMembershipDto>>(
                $"/api/v1/families/{familyId}", cancellationToken);
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family for FamilyId: {FamilyId}", familyId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CompanyDepartmentMembersDto>?> GetDepartmentsWithMembersAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OrganizationApiResponse<OrganizationPagedResult<CompanyDepartmentMembersDto>>>(
                $"/api/v1/departments?organizationId={companyId}&pageIndex=1&pageSize=100", cancellationToken);
            return response?.Data?.Items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve departments for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<CompanyLicenseDto?> GetCompanyLicensesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OrganizationApiResponse<CompanyLicenseDto>>(
                $"/api/v1/licenses/company/{companyId}", cancellationToken);
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve licenses for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<PlatformStatsDto?> GetPlatformStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PlatformStatsDto>(
                "/api/v1/platform/stats", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve platform statistics");
            return null;
        }
    }

    private sealed class OrganizationApiResponse<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }
    }

    private sealed class OrganizationPagedResult<T>
    {
        public List<T>? Items { get; set; }
    }
}
