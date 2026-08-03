using LifeBalance.Reporting.Domain.Enums;

namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// Persists an audit entry for every report generation request.
/// The Reporting service stores no business data; only operational logs.
/// </summary>
public interface IReportGenerationLogService
{
    /// <summary>Records a report generation request. Failures to persist are logged but never surfaced.</summary>
    Task LogAsync(
        ReportScope scope,
        string? scopeId,
        string userId,
        ReportFormat? format,
        ReportStatus status,
        double durationMs,
        int recordCount,
        string? errorMessage = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
