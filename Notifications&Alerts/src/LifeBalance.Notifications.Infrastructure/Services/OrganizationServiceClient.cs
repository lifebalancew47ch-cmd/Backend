using System.Net.Http.Json;
using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly HttpClient _httpClient;

    public OrganizationServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:OrganizationService"] ?? "http://localhost:5002");
    }

    public async Task<OrganizationInfo?> GetOrganizationAsync(string organizationId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/organizations/{organizationId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrganizationInfo>();
    }

    public async Task<FamilyInfo?> GetFamilyAsync(string familyId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/families/{familyId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FamilyInfo>();
    }

    public async Task<DepartmentInfo?> GetDepartmentAsync(string departmentId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/departments/{departmentId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DepartmentInfo>();
    }

    public async Task<List<string>> GetOrganizationMembersAsync(string organizationId)
    {
        var org = await GetOrganizationAsync(organizationId);
        return org?.MemberIds ?? new List<string>();
    }

    public async Task<List<string>> GetFamilyMembersAsync(string familyId)
    {
        var family = await GetFamilyAsync(familyId);
        return family?.MemberIds ?? new List<string>();
    }

    public async Task<List<string>> GetDepartmentMembersAsync(string departmentId)
    {
        var dept = await GetDepartmentAsync(departmentId);
        return dept?.MemberIds ?? new List<string>();
    }
}
