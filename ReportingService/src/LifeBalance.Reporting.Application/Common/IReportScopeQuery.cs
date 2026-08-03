using LifeBalance.Reporting.Domain.Enums;

namespace LifeBalance.Reporting.Application.Common;

/// <summary>
/// Shared contract implemented by all scoped report queries, enabling common validation
/// of scope, scope identifier and date range.
/// </summary>
public interface IReportScopeQuery
{
    /// <summary>Gets the report scope.</summary>
    ReportScope Scope { get; }

    /// <summary>Gets the scope identifier (familyId/companyId; userId for individual reports).</summary>
    string? ScopeId { get; }

    /// <summary>Gets the optional inclusive start of the date range.</summary>
    DateTime? From { get; }

    /// <summary>Gets the optional inclusive end of the date range.</summary>
    DateTime? To { get; }
}
