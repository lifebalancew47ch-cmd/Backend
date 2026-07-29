namespace LifeBalance.Notifications.Application.Interfaces;

public class OrganizationInfo
{
    public string OrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
}

public class FamilyInfo
{
    public string FamilyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
}

public class DepartmentInfo
{
    public string DepartmentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
}

public interface IOrganizationServiceClient
{
    Task<OrganizationInfo?> GetOrganizationAsync(string organizationId);
    Task<FamilyInfo?> GetFamilyAsync(string familyId);
    Task<DepartmentInfo?> GetDepartmentAsync(string departmentId);
    Task<List<string>> GetOrganizationMembersAsync(string organizationId);
    Task<List<string>> GetFamilyMembersAsync(string familyId);
    Task<List<string>> GetDepartmentMembersAsync(string departmentId);
}
