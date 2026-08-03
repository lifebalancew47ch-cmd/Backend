namespace LifeBalance.Reporting.API.Authorization;

/// <summary>
/// Centralized definition of all Role names used in the Reporting API.
/// NormalizedName (UPPERCASE) values are emitted by the Auth &amp; Profile service.
/// </summary>
public static class Roles
{
    /// <summary>Administrator role with full access.</summary>
    public const string Admin = "ADMIN";

    /// <summary>Standard authenticated user.</summary>
    public const string User = "USER";

    /// <summary>Read-only viewer role.</summary>
    public const string Viewer = "VIEWER";
}
