using LifeBalance.Reporting.Application.Common.Interfaces;

namespace LifeBalance.Reporting.Application.Common;

/// <summary>
/// Describes a single reportable metric: its API code, display name and an extractor
/// that pulls the raw value from a <see cref="MedicalReadingDto"/>.
/// </summary>
public sealed record ReportMetricDefinition(
    string Code,
    string DisplayName,
    Func<MedicalReadingDto, double?> Extractor);

/// <summary>
/// Central registry of the metrics that can be reported, exported and analyzed.
/// </summary>
public static class ReportMetrics
{
    /// <summary>All supported metrics.</summary>
    public static readonly IReadOnlyList<ReportMetricDefinition> All =
    [
        new("steps", "Steps", reading => reading.Steps),
        new("heartrate", "Heart Rate", reading => reading.HeartRate),
        new("hrv", "HRV", reading => reading.Hrv),
        new("spo2", "SpO2", reading => reading.Spo2),
        new("systolicbp", "Systolic BP", reading => reading.SystolicBp),
        new("diastolicbp", "Diastolic BP", reading => reading.DiastolicBp),
        new("weight", "Weight", reading => reading.Weight),
        new("height", "Height", reading => reading.Height)
    ];

    private static readonly Dictionary<string, ReportMetricDefinition> ByCode =
        All.ToDictionary(m => m.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Attempts to resolve a metric definition by its code.</summary>
    public static bool TryGet(string code, out ReportMetricDefinition metric)
        => ByCode.TryGetValue(code, out metric!);

    /// <summary>
    /// Resolves a list of metric codes to their definitions. Empty input resolves to
    /// <see cref="All"/>; unknown codes are silently ignored.
    /// </summary>
    public static IReadOnlyList<ReportMetricDefinition> Resolve(IReadOnlyList<string>? codes)
    {
        if (codes is null || codes.Count == 0)
        {
            return All;
        }

        var resolved = new List<ReportMetricDefinition>();
        foreach (var code in codes)
        {
            if (ByCode.TryGetValue(code, out var metric) && !resolved.Contains(metric))
            {
                resolved.Add(metric);
            }
        }

        return resolved.Count == 0 ? All : resolved;
    }
}
