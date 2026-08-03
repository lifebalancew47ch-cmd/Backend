namespace LifeBalance.Reporting.API.Authorization;

/// <summary>
/// Centralized definition of all Authorization Policy names used in the Reporting API.
/// Use these constants with <c>[Authorize(Policy = Policies.XXX)]</c>.
/// </summary>
public static class Policies
{
    /// <summary>Policy requiring any authenticated user.</summary>
    public const string AuthenticatedUser = "AuthenticatedUser";

    /// <summary>Policy requiring a user with the Admin role.</summary>
    public const string Admin = "Admin";

    /// <summary>Policy allowing authenticated users to read reports.</summary>
    public const string ReportRead = "ReportRead";

    /// <summary>Policy allowing users to export report documents.</summary>
    public const string ReportExport = "ReportExport";
}
