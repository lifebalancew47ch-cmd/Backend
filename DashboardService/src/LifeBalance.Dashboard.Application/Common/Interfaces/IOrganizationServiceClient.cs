namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record DepartmentSummaryDto(string DepartmentId, string Name, int TotalMembers, double ActiveAdherenceScore);
public record CompanyLicenseDto(string CompanyId, int TotalLicenses, int UsedLicenses, DateTime ExpirationDateUtc, string PlanType);
public record FamilyMembershipDto(string FamilyId, string AdministratorUserId, List<string> MemberUserIds);
public record CompanyDepartmentMembersDto(string DepartmentId, string DepartmentName, List<string> MemberUserIds);

public interface IOrganizationServiceClient
{
    Task<List<DepartmentSummaryDto>?> GetDepartmentsAsync(string companyId, CancellationToken cancellationToken = default);
    Task<CompanyLicenseDto?> GetCompanyLicensesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<FamilyMembershipDto?> GetFamilyByIdAsync(string familyId, CancellationToken cancellationToken = default);
    Task<List<CompanyDepartmentMembersDto>?> GetCompanyDepartmentsWithMembersAsync(string companyId, CancellationToken cancellationToken = default);
}
