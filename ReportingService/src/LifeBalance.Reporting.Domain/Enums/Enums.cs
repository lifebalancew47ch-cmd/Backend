namespace LifeBalance.Reporting.Domain.Enums;

/// <summary>
/// The scope of a report: individual, family or company.
/// </summary>
public enum ReportScope
{
    /// <summary>A single user report.</summary>
    Individual = 0,

    /// <summary>A family report.</summary>
    Family = 1,

    /// <summary>A company / organizational report.</summary>
    Company = 2
}

/// <summary>
/// The downloadable document format of a report.
/// </summary>
public enum ReportFormat
{
    /// <summary>Portable Document Format.</summary>
    Pdf = 0,

    /// <summary>Excel OpenXML spreadsheet (.xlsx).</summary>
    Excel = 1,

    /// <summary>Comma separated values (.csv).</summary>
    Csv = 2
}

/// <summary>
/// The processing status of a report generation request.
/// </summary>
public enum ReportStatus
{
    /// <summary>The report was generated successfully.</summary>
    Completed = 0,

    /// <summary>The report generation failed.</summary>
    Failed = 1
}

/// <summary>
/// The period used to aggregate historical statistics.
/// </summary>
public enum AggregationPeriod
{
    /// <summary>Aggregate per day.</summary>
    Daily = 0,

    /// <summary>Aggregate per ISO week.</summary>
    Weekly = 1,

    /// <summary>Aggregate per calendar month.</summary>
    Monthly = 2
}

/// <summary>
/// The direction of a historical trend (linear regression slope sign).
/// </summary>
public enum TrendDirection
{
    /// <summary>The trend is flat (no significant change).</summary>
    Stable = 0,

    /// <summary>The values are increasing over time.</summary>
    Increasing = 1,

    /// <summary>The values are decreasing over time.</summary>
    Decreasing = 2
}
