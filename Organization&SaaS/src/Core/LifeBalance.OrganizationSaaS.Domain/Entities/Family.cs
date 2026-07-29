using LifeBalance.OrganizationSaaS.Domain.Common;

namespace LifeBalance.OrganizationSaaS.Domain.Entities;

public class Family : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string AdministratorUserId { get; private set; } = string.Empty;
    public List<string> MemberUserIds { get; private set; } = new();
    public int MaxMembers { get; private set; } = 6;

    private Family() { }

    public Family(string name, string administratorUserId, string tenantId, int maxMembers = 6)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Family name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(administratorUserId))
            throw new ArgumentException("AdministratorUserId is required.", nameof(administratorUserId));

        Name = name;
        AdministratorUserId = administratorUserId;
        TenantId = tenantId;
        MaxMembers = maxMembers;
        MemberUserIds.Add(administratorUserId);
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Family name cannot be empty.");

        Name = name;
        Touch();
    }

    public void AddMember(string userId)
    {
        if (MemberUserIds.Count >= MaxMembers)
            throw new InvalidOperationException($"Family member limit of {MaxMembers} reached.");
        if (MemberUserIds.Contains(userId))
            throw new InvalidOperationException("User is already a member of this family.");

        MemberUserIds.Add(userId);
        Touch();
    }

    public void RemoveMember(string userId)
    {
        if (userId == AdministratorUserId)
            throw new InvalidOperationException("Cannot remove the family administrator. Transfer admin role first.");

        MemberUserIds.Remove(userId);
        Touch();
    }

    public void TransferAdmin(string newAdminUserId)
    {
        if (!MemberUserIds.Contains(newAdminUserId))
            throw new InvalidOperationException("New administrator must be a member of the family.");

        AdministratorUserId = newAdminUserId;
        Touch();
    }
}
