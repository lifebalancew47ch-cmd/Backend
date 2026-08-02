namespace LifeBalance.Administration.Application.Common.Constants;

/// <summary>
/// Administrative roles allowed to use the Administration Service and the
/// authorization policy name that protects every endpoint.
/// </summary>
public static class AdministrationRoles
{
    public const string SuperAdmin = "SUPERADMIN";
    public const string SystemAdministrator = "SYSTEMADMINISTRATOR";

    /// <summary>Policy applied to every controller (fallback + explicit).</summary>
    public const string AdministratorOnlyPolicy = "AdministratorOnly";

    /// <summary>Role values honoured by the administrator policy.</summary>
    public static readonly IReadOnlyList<string> AllowedAdministrators =
        new[] { SuperAdmin, SystemAdministrator };
}
