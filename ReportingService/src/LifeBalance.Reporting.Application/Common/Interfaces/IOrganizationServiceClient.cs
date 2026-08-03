namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// Organization summary as modeled by the Organization &amp; SaaS service.
/// </summary>
public sealed record CompanyDto(
    string Id,
    string Name,
    string? Industry,
    int TotalEmployees,
    string PlanType,
    DateTime? LicenseExpirationUtc);

/// <summary>
/// Family membership information.
/// </summary>
public sealed record FamilyMembershipDto(
    string FamilyId,
    string AdministratorUserId,
    IReadOnlyList<string> MemberUserIds);

/// <summary>
/// A company department together with its member user identifiers.
/// </summary>
public sealed record CompanyDepartmentMembersDto(
    string DepartmentId,
    string DepartmentName,
    IReadOnlyList<string> MemberUserIds);

/// <summary>
/// License usage summary for a company.
/// </summary>
public sealed record CompanyLicenseDto(
    string CompanyId,
    int TotalLicenses,
    int UsedLicenses,
    DateTime ExpirationDateUtc,
    string PlanType);

/// <summary>
/// Global platform statistics.
/// </summary>
public sealed record PlatformStatsDto(
    int TotalUsers,
    int TotalCompanies,
    int TotalFamilies,
    int ActiveLicenses);

/// <summary>
/// Contract for the Organization &amp; SaaS microservice client.
/// All methods return <c>null</c> when the upstream call fails (fail-closed callers).
/// </summary>
public interface IOrganizationServiceClient
{
    /// <summary>Retrieves a company by identifier.</summary>
    Task<CompanyDto?> GetCompanyAsync(string companyId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a family and its membership.</summary>
    Task<FamilyMembershipDto?> GetFamilyAsync(string familyId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all departments with their member user identifiers for a company.</summary>
    Task<IReadOnlyList<CompanyDepartmentMembersDto>?> GetDepartmentsWithMembersAsync(string companyId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the license usage summary for a company.</summary>
    Task<CompanyLicenseDto?> GetCompanyLicensesAsync(string companyId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves global platform statistics.</summary>
    Task<PlatformStatsDto?> GetPlatformStatsAsync(CancellationToken cancellationToken = default);
}
