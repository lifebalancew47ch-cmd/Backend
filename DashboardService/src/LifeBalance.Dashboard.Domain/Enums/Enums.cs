namespace LifeBalance.Dashboard.Domain.Enums;

/// <summary>
/// Dashboard categorization type.
/// </summary>
public enum DashboardType
{
    Individual,
    Family,
    Company,
    General
}

/// <summary>
/// Status of an aggregation execution.
/// </summary>
public enum AggregationStatus
{
    Success,
    PartialSuccess,
    Failed
}

/// <summary>
/// Time filter range for metrics.
/// </summary>
public enum TimeRange
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}
