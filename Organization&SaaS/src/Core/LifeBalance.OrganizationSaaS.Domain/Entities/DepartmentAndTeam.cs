using LifeBalance.OrganizationSaaS.Domain.Common;

namespace LifeBalance.OrganizationSaaS.Domain.Entities;

public class Department : AggregateRoot
{
    public string OrganizationId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ManagerUserId { get; private set; }
    public string? ParentDepartmentId { get; private set; }
    public List<string> MemberUserIds { get; private set; } = new();

    private Department() { }

    public Department(string organizationId, string name, string description, string tenantId, string? managerUserId = null, string? parentDepartmentId = null)
    {
        OrganizationId = organizationId;
        Name = name;
        Description = description;
        TenantId = tenantId;
        ManagerUserId = managerUserId;
        ParentDepartmentId = parentDepartmentId;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description, string? managerUserId, string? parentDepartmentId)
    {
        Name = name;
        Description = description;
        ManagerUserId = managerUserId;
        ParentDepartmentId = parentDepartmentId;
        Touch();
    }

    public void AddMember(string userId)
    {
        if (!MemberUserIds.Contains(userId))
        {
            MemberUserIds.Add(userId);
            Touch();
        }
    }

    public void RemoveMember(string userId)
    {
        if (MemberUserIds.Remove(userId))
        {
            Touch();
        }
    }
}

public class Team : AggregateRoot
{
    public string OrganizationId { get; private set; } = string.Empty;
    public string? DepartmentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? LeaderUserId { get; private set; }
    public List<string> MemberUserIds { get; private set; } = new();

    private Team() { }

    public Team(string organizationId, string name, string tenantId, string? departmentId = null, string? leaderUserId = null)
    {
        OrganizationId = organizationId;
        Name = name;
        TenantId = tenantId;
        DepartmentId = departmentId;
        LeaderUserId = leaderUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? departmentId, string? leaderUserId)
    {
        Name = name;
        DepartmentId = departmentId;
        LeaderUserId = leaderUserId;
        Touch();
    }

    public void AddMember(string userId)
    {
        if (!MemberUserIds.Contains(userId))
        {
            MemberUserIds.Add(userId);
            Touch();
        }
    }

    public void RemoveMember(string userId)
    {
        if (MemberUserIds.Remove(userId))
        {
            Touch();
        }
    }
}
