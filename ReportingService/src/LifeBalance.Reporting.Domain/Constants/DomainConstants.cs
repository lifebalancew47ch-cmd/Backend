namespace LifeBalance.Reporting.Domain.Constants;

/// <summary>
/// Application-wide constants for the Reporting Domain.
/// </summary>
public static class DomainConstants
{
    /// <summary>The bounded context name.</summary>
    public const string ServiceName = "LifeBalance.ReportingService";

    /// <summary>The default MongoDB database name.</summary>
    public const string DatabaseName = "lifebalance_reporting";

    /// <summary>The MongoDB collection that stores report generation audit logs.</summary>
    public const string ReportLogsCollection = "report_generation_logs";

    /// <summary>Minimum allowed string length for names and identifiers.</summary>
    public const int MinNameLength = 2;

    /// <summary>Maximum allowed string length for names.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Default number of days included when no date range is supplied.</summary>
    public const int DefaultReportDays = 30;

    /// <summary>Maximum number of days a single report range may span.</summary>
    public const int MaxReportDays = 366;

    /// <summary>Maximum number of upstream readings requested per report.</summary>
    public const int MaxReadingsPerReport = 5000;
}
