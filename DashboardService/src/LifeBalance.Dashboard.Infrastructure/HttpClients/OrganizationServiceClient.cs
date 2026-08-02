using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationServiceClient> _logger;

    public OrganizationServiceClient(HttpClient httpClient, ILogger<OrganizationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DepartmentSummaryDto>?> GetDepartmentsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<DepartmentSummaryDto>>($"/api/v1/org/companies/{companyId}/departments", cancellationToken);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve departments for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<CompanyLicenseDto?> GetCompanyLicensesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CompanyLicenseDto>($"/api/v1/org/companies/{companyId}/licenses", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve license info for CompanyId: {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<FamilyMembershipDto?> GetFamilyByIdAsync(string familyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<OrganizationApiResponse<FamilyMembershipDto>>($"/api/v1/families/{familyId}", cancellationToken);
            return res?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve family membership for FamilyId: {FamilyId}", familyId);
            return null;
        }
    }

    public async Task<List<CompanyDepartmentMembersDto>?> GetCompanyDepartmentsWithMembersAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<OrganizationApiResponse<OrganizationPagedResult<CompanyDepartmentMembersDto>>>(
                $"/api/v1/departments?organizationId={companyId}&pageIndex=1&pageSize=100", cancellationToken);
            return res?.Data?.Items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve company departments with members for CompanyId: {CompanyId}", companyId);
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
