namespace LifeBalance.Dashboard.API.Authorization;

/// <summary>
/// Centralized definition of all Authorization Policy names used in the Dashboard API.
/// Use these constants with <c>[Authorize(Policy = Policies.XXX)]</c>.
/// </summary>
public static class Policies
{
    /// <summary>Policy requiring any authenticated user.</summary>
    public const string AuthenticatedUser = "AuthenticatedUser";

    /// <summary>Policy allowing only users with the Admin role.</summary>
    public const string Admin = "Admin";

    /// <summary>Policy allowing users with the Dashboard:Read permission.</summary>
    public const string DashboardRead = "DashboardRead";

    /// <summary>Policy allowing users with the Dashboard:Write permission.</summary>
    public const string DashboardWrite = "DashboardWrite";
}
