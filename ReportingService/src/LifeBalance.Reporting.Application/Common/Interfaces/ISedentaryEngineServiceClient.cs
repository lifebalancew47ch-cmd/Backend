namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// A daily sedentary engine record.
/// </summary>
public sealed record SedentaryDailyDto(
    DateTime Date,
    double SedentaryScore,
    double SedentaryHours,
    double ActiveMinutes,
    int Steps,
    int BreakCount);

/// <summary>
/// A user goal as tracked by the sedentary engine.
/// </summary>
public sealed record GoalDto(
    string Id,
    string Name,
    string Metric,
    double Target,
    double Current,
    bool Completed,
    DateTime? CompletedAtUtc);

/// <summary>
/// Per-member compliance for a family.
/// </summary>
public sealed record MemberComplianceDto(string UserId, double CompliancePercentage);

/// <summary>
/// Family compliance summary.
/// </summary>
public sealed record FamilyComplianceDto(
    string FamilyId,
    double CompliancePercentage,
    int ActiveMembers,
    int TotalMembers,
    IReadOnlyList<MemberComplianceDto> Members);

/// <summary>
/// Per-department adherence for a company.
/// </summary>
public sealed record DepartmentAdherenceDto(
    string DepartmentId,
    double AdherencePercentage,
    int TotalEmployees,
    int ActiveEmployees);

/// <summary>
/// Company adherence summary.
/// </summary>
public sealed record CompanyAdherenceDto(
    string CompanyId,
    double AdherencePercentage,
    int TotalEmployees,
    int ActiveEmployees,
    IReadOnlyList<string> HighRiskDepartments,
    IReadOnlyList<DepartmentAdherenceDto> Departments);

/// <summary>
/// Latest sedentary score snapshot for a user.
/// </summary>
public sealed record SedentaryScoreDto(
    string UserId,
    double DailySteps,
    double ActiveMinutes,
    double SedentaryHours,
    double CaloriesBurned,
    double Score);

/// <summary>
/// Contract for the Sedentary Engine microservice client.
/// All methods return <c>null</c> when the upstream call fails (fail-closed callers).
/// </summary>
public interface ISedentaryEngineServiceClient
{
    /// <summary>Retrieves the latest sedentary score snapshot for a user.</summary>
    Task<SedentaryScoreDto?> GetUserScoreAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the daily sedentary history of a user within a date range.</summary>
    Task<IReadOnlyList<SedentaryDailyDto>?> GetUserHistoryAsync(
        string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the active goals of a user.</summary>
    Task<IReadOnlyList<GoalDto>?> GetUserGoalsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the compliance of a family within a date range.</summary>
    Task<FamilyComplianceDto?> GetFamilyComplianceAsync(
        string familyId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the adherence of a company within a date range.</summary>
    Task<CompanyAdherenceDto?> GetCompanyAdherenceAsync(
        string companyId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
