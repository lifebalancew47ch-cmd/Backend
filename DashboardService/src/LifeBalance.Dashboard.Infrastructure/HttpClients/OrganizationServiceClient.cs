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

    public async Task<List<DepartmentSummaryDto>> GetDepartmentsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<DepartmentSummaryDto>>($"/api/v1/org/companies/{companyId}/departments", cancellationToken);
            return res ?? GetFallbackDepartments();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve departments for CompanyId: {CompanyId}", companyId);
            return GetFallbackDepartments();
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
            return new CompanyLicenseDto(companyId, 250, 180, DateTime.UtcNow.AddYears(1), "Enterprise Pro");
        }
    }

    private static List<DepartmentSummaryDto> GetFallbackDepartments() => new()
    {
        new DepartmentSummaryDto("dept_1", "Engineering", 45, 91.2),
        new DepartmentSummaryDto("dept_2", "Human Resources", 15, 88.5),
        new DepartmentSummaryDto("dept_3", "Marketing", 20, 84.0)
    };
}
