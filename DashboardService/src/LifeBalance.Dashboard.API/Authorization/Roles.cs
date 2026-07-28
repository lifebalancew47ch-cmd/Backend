namespace LifeBalance.Dashboard.API.Authorization;

/// <summary>
/// Centralized definition of all Role names used in the Dashboard API.
/// </summary>
public static class Roles
{
    /// <summary>Administrator role with full access.</summary>
    public const string Admin = "Admin";

    /// <summary>Standard authenticated user.</summary>
    public const string User = "User";

    /// <summary>Read-only access to dashboard data.</summary>
    public const string Viewer = "Viewer";
}
