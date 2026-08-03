using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.ValueObjects;

namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// A fully validated and authorized dataset ready to be analyzed by report handlers.
/// </summary>
public sealed record ReportDataset(
    ReportScope Scope,
    string ScopeId,
    DateTime From,
    DateTime To,
    IReadOnlyList<MedicalReadingDto> Readings,
    AuthUserProfileDto? UserProfile,
    IReadOnlyList<AuthUserProfileDto> Members,
    CompanyDto? Company,
    IReadOnlyList<CompanyDepartmentMembersDto> Departments,
    FamilyMembershipDto? Family);

/// <summary>
/// Builds an authorized report dataset for a scope, consolidating data from the
/// Medical Data, Auth, Organization and Dashboard microservices via REST clients.
/// </summary>
public interface IReportDatasetService
{
    /// <summary>
    /// Resolves the scope identifier (userId for individual reports, familyId/companyId
    /// for scoped reports), validates the requester's membership (anti-IDOR) and
    /// consolidates the raw source data. Throws <see cref="Exceptions.ReportAccessDeniedException"/>
    /// (403) for unauthorized access and <see cref="Exceptions.UpstreamServiceUnavailableException"/>
    /// (503) when an upstream service is unavailable.
    /// </summary>
    Task<ReportDataset> BuildAsync(
        ReportScope scope,
        string? requestedId,
        string requesterUserId,
        IReadOnlyList<string> requesterRoles,
        DateRange range,
        CancellationToken cancellationToken = default);
}
