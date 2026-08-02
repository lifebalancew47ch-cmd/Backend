namespace LifeBalance.Dashboard.API.Authorization;

/// <summary>
/// Centralized definition of all Role names used in the Dashboard API.
/// </summary>
public static class Roles
{
    /// <summary>Administrator role with full access. NormalizedName (MAYUSCULAS) emitido por Auth.</summary>
    public const string Admin = "ADMIN";

    /// <summary>Standard authenticated user. NormalizedName (MAYUSCULAS) emitido por Auth.</summary>
    public const string User = "USER";

    /// <summary>Read-only access to dashboard data. NormalizedName (MAYUSCULAS) emitido por Auth.</summary>
    public const string Viewer = "VIEWER";
}
