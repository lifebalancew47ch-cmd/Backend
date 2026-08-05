using LifeBalance.OrganizationSaaS.Domain.Common;
using LifeBalance.OrganizationSaaS.Domain.Enums;

namespace LifeBalance.OrganizationSaaS.Domain.Entities;

public class License : AggregateRoot
{
    public string OrganizationId { get; private set; } = string.Empty;
    public string LicenseKey { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty; // e.g. "Standard", "Pro", "Enterprise"
    public LicenseStatus Status { get; private set; } = LicenseStatus.Available;
    public string? AssignedUserId { get; private set; }
    public DateTime IssuedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }

    private License() { }

    public License(string organizationId, string type, DateTime expiresAt, string tenantId)
    {
        OrganizationId = organizationId;
        Type = type;
        ExpiresAt = expiresAt;
        TenantId = tenantId;
        LicenseKey = Guid.NewGuid().ToString("N").ToUpper();
        Status = LicenseStatus.Available;
        IssuedAt = DateTime.UtcNow;
    }

    public void AssignToUser(string userId)
    {
        if (Status != LicenseStatus.Available)
            throw new InvalidOperationException("License is not available for assignment.");

        AssignedUserId = userId;
        Status = LicenseStatus.Assigned;
        Touch();
    }

    public void Revoke()
    {
        AssignedUserId = null;
        Status = LicenseStatus.Revoked;
        Touch();
    }

    public void Renew(DateTime newExpiration)
    {
        ExpiresAt = newExpiration;
        if (Status == LicenseStatus.Expired)
        {
            Status = AssignedUserId != null ? LicenseStatus.Assigned : LicenseStatus.Available;
        }
        Touch();
    }
}

public class Membership : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string? OrganizationId { get; private set; }
    public string? FamilyId { get; private set; }
    public string PlanId { get; private set; } = string.Empty;
    public MemberRole Role { get; private set; } = MemberRole.Member;
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; private set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; private set; }

    private Membership() { }

    public Membership(string userId, string planId, string tenantId, string? organizationId = null, string? familyId = null, MemberRole role = MemberRole.Member)
    {
        UserId = userId;
        PlanId = planId;
        TenantId = tenantId;
        OrganizationId = organizationId;
        FamilyId = familyId;
        Role = role;
        Status = SubscriptionStatus.Active;
        StartDate = DateTime.UtcNow;
    }

    public void UpdatePlan(string newPlanId)
    {
        PlanId = newPlanId;
        Touch();
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Canceled;
        EndDate = DateTime.UtcNow;
        Touch();
    }
}

public class Subscription : AggregateRoot
{
    public string OrganizationId { get; private set; } = string.Empty;
    public string PlanId { get; private set; } = string.Empty;
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.Active;
    public DateTime RenewalDate { get; private set; }
    public string BillingCycle { get; private set; } = "Monthly"; // Monthly, Yearly
    public List<string> PaymentHistoryLog { get; private set; } = new();

    private Subscription() { }

    public Subscription(string organizationId, string planId, string billingCycle, string tenantId)
    {
        OrganizationId = organizationId;
        PlanId = planId;
        BillingCycle = billingCycle;
        TenantId = tenantId;
        Status = SubscriptionStatus.Active;
        RenewalDate = billingCycle == "Yearly" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
    }

    public void Renew()
    {
        RenewalDate = BillingCycle == "Yearly" ? RenewalDate.AddYears(1) : RenewalDate.AddMonths(1);
        Status = SubscriptionStatus.Active;
        PaymentHistoryLog.Add($"Renewed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Touch();
    }

    public void ChangePlan(string newPlanId)
    {
        PlanId = newPlanId;
        PaymentHistoryLog.Add($"Plan changed to {newPlanId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Touch();
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Canceled;
        PaymentHistoryLog.Add($"Canceled on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Touch();
    }
}

public class Invitation : AggregateRoot
{
    public string TargetEmail { get; private set; } = string.Empty;
    public string? OrganizationId { get; private set; }
    public string? FamilyId { get; private set; }
    public MemberRole Role { get; private set; } = MemberRole.Member;
    public string Token { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;
    public DateTime SentAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }

    private Invitation() { }

    public Invitation(string targetEmail, string tenantId, string? organizationId = null, string? familyId = null, MemberRole role = MemberRole.Member)
    {
        TargetEmail = targetEmail;
        TenantId = tenantId;
        OrganizationId = organizationId;
        FamilyId = familyId;
        Role = role;
        Token = Guid.NewGuid().ToString("N");
        Status = InvitationStatus.Pending;
        SentAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(7);
    }

    public void Accept()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Only pending invitations can be accepted.");
        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = InvitationStatus.Expired;
            throw new InvalidOperationException("Invitation has expired.");
        }

        Status = InvitationStatus.Accepted;
        Touch();
    }

    public void Reject()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Only pending invitations can be rejected.");

        Status = InvitationStatus.Rejected;
        Touch();
    }

    public void Cancel()
    {
        Status = InvitationStatus.Canceled;
        Touch();
    }

    public void Resend()
    {
        Token = Guid.NewGuid().ToString("N");
        SentAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(7);
        Status = InvitationStatus.Pending;
        Touch();
    }
}

public class OrganizationConfig : AggregateRoot
{
    public string OrganizationId { get; private set; } = string.Empty;
    public string Language { get; set; } = "es";
    public string TimeZone { get; set; } = "America/Mexico_City";
    public string PoliciesJson { get; set; } = "{}";
    public string GoalsJson { get; set; } = "{}";

    private OrganizationConfig() { }

    public OrganizationConfig(string organizationId, string tenantId)
    {
        OrganizationId = organizationId;
        TenantId = tenantId;
    }
}

public class AuditLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public AuditLog() { }

    public AuditLog(string userId, string action, string entityName, string entityId, string changesJson, string correlationId)
    {
        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        ChangesJson = changesJson;
        CorrelationId = correlationId;
        TenantId = "GLOBAL";
    }
}
