namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// Compact KPIs aggregated by the Dashboard service for a scope.
/// </summary>
public sealed record DashboardKpisDto(
    string? UserId,
    string? FamilyId,
    string? CompanyId,
    double AverageDailySteps,
    double AverageHeartRate,
    double SedentaryHours,
    double AdherenceRate,
    DateTime GeneratedAtUtc);

/// <summary>
/// Generic dashboard summary returned by the Dashboard service.
/// </summary>
public sealed record DashboardSummaryDto(
    string Scope,
    string? ScopeId,
    int TotalUsers,
    double GlobalHealthScore,
    int ActiveToday,
    DateTime GeneratedAtUtc);

/// <summary>
/// Contract for the Dashboard microservice client.
/// All methods return <c>null</c> when the upstream call fails (fail-closed callers).
/// </summary>
public interface IDashboardServiceClient
{
    /// <summary>Retrieves KPIs for the given scope ("individual" | "family" | "company").</summary>
    Task<DashboardKpisDto?> GetKpisAsync(string scope, string? scopeId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a generic dashboard summary for the given scope.</summary>
    Task<DashboardSummaryDto?> GetDashboardSummaryAsync(string scope, string? scopeId, CancellationToken cancellationToken = default);
}
