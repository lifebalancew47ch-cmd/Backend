namespace LifeBalance.Dashboard.Domain.Constants;

/// <summary>
/// Application-wide constants for the Dashboard Domain.
/// </summary>
public static class DomainConstants
{
    /// <summary>The bounded context name.</summary>
    public const string ServiceName = "LifeBalance.DashboardService";

    /// <summary>The default MongoDB database name.</summary>
    public const string DatabaseName = "lifebalance_dashboard";

    /// <summary>Minimum allowed string length for names and identifiers.</summary>
    public const int MinNameLength = 2;

    /// <summary>Maximum allowed string length for names.</summary>
    public const int MaxNameLength = 200;
}
