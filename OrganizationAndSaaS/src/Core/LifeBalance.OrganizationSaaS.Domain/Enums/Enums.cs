namespace LifeBalance.OrganizationSaaS.Domain.Enums;

public enum OrganizationStatus
{
    Active = 1,
    Suspended = 2,
    Blocked = 3,
    Restored = 4
}

public enum PlanTier
{
    Free = 1,
    Personal = 2,
    Family = 3,
    Business = 4,
    Enterprise = 5
}

public enum LicenseStatus
{
    Available = 1,
    Assigned = 2,
    Expired = 3,
    Revoked = 4
}

public enum SubscriptionStatus
{
    Active = 1,
    PendingPayment = 2,
    Expired = 3,
    Canceled = 4
}

public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    Canceled = 5
}

public enum MemberRole
{
    Owner = 1,
    Admin = 2,
    Manager = 3,
    Leader = 4,
    Member = 5,
    Viewer = 6
}
