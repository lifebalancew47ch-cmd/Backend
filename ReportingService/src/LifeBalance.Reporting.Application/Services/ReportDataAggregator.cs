using FluentValidation.Results;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.ValueObjects;
using ValidationException = LifeBalance.Reporting.Application.Exceptions.ValidationException;

namespace LifeBalance.Reporting.Application.Services;

/// <summary>
/// Consolidates the raw data required by report handlers from the upstream microservices
/// and enforces scope-level authorization (anti-IDOR / broken access control prevention).
/// </summary>
public sealed class ReportDataAggregator : IReportDatasetService
{
    private const string AdminRole = "ADMIN";

    private readonly IAuthServiceClient _authClient;
    private readonly IMedicalDataServiceClient _medicalClient;
    private readonly IOrganizationServiceClient _organizationClient;

    /// <summary>Initializes a new instance of <see cref="ReportDataAggregator"/>.</summary>
    public ReportDataAggregator(
        IAuthServiceClient authClient,
        IMedicalDataServiceClient medicalClient,
        IOrganizationServiceClient organizationClient)
    {
        _authClient = authClient;
        _medicalClient = medicalClient;
        _organizationClient = organizationClient;
    }

    /// <inheritdoc/>
    public async Task<ReportDataset> BuildAsync(
        ReportScope scope,
        string? requestedId,
        string requesterUserId,
        IReadOnlyList<string> requesterRoles,
        DateRange range,
        CancellationToken cancellationToken)
    {
        var scopeId = scope == ReportScope.Individual ? requesterUserId : requestedId;

        if (string.IsNullOrWhiteSpace(scopeId))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("ScopeId", "ScopeId is required for family and company reports.")
            });
        }

        return scope switch
        {
            ReportScope.Individual => await BuildIndividualAsync(requesterUserId, range, cancellationToken),
            ReportScope.Family => await BuildFamilyAsync(scopeId!, requesterUserId, requesterRoles, range, cancellationToken),
            ReportScope.Company => await BuildCompanyAsync(scopeId!, requesterUserId, requesterRoles, range, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }

    private async Task<ReportDataset> BuildIndividualAsync(
        string userId,
        DateRange range,
        CancellationToken cancellationToken)
    {
        var profile = await _authClient.GetUserProfileAsync(userId, cancellationToken);
        if (profile == null) throw new UpstreamServiceUnavailableException($"The user profile for '{userId}' is unavailable.");

        var readings = await _medicalClient.GetUserReadingsAsync(userId, range.From, range.To, cancellationToken)
            ?? new List<MedicalReadingDto>();

        return new ReportDataset(
            ReportScope.Individual,
            userId,
            range.From,
            range.To,
            readings,
            profile,
            [],
            null,
            [],
            null);
    }

    private async Task<ReportDataset> BuildFamilyAsync(
        string familyId,
        string requesterUserId,
        IReadOnlyList<string> requesterRoles,
        DateRange range,
        CancellationToken cancellationToken)
    {
        var family = await _organizationClient.GetFamilyAsync(familyId, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"The family '{familyId}' is unavailable.");

        var isAuthorized = requesterRoles.Contains(AdminRole)
            || requesterUserId == family.AdministratorUserId
            || family.MemberUserIds.Contains(requesterUserId);

        if (!isAuthorized)
        {
            throw new ReportAccessDeniedException(
                $"The current user is not a member of family '{familyId}'.");
        }

        var members = await _authClient.GetFamilyMembersAsync(familyId, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"The members of family '{familyId}' are unavailable.");

        var readings = await _medicalClient.GetFamilyReadingsAsync(familyId, range.From, range.To, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"The biometric history for family '{familyId}' is unavailable.");

        return new ReportDataset(
            ReportScope.Family,
            familyId,
            range.From,
            range.To,
            readings,
            null,
            members,
            null,
            [],
            family);
    }

    private async Task<ReportDataset> BuildCompanyAsync(
        string companyId,
        string requesterUserId,
        IReadOnlyList<string> requesterRoles,
        DateRange range,
        CancellationToken cancellationToken)
    {
        var company = await _organizationClient.GetCompanyAsync(companyId, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"The company '{companyId}' is unavailable.");

        var departments = await _organizationClient.GetDepartmentsWithMembersAsync(companyId, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"The departments of company '{companyId}' are unavailable.");

        var isMember = departments.Any(d => d.MemberUserIds.Contains(requesterUserId));
        if (!requesterRoles.Contains(AdminRole) && !isMember)
        {
            throw new ReportAccessDeniedException(
                $"The current user does not belong to company '{companyId}'.");
        }

        var readings = await _medicalClient.GetCompanyReadingsAsync(companyId, range.From, range.To, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"The biometric history for company '{companyId}' is unavailable.");

        return new ReportDataset(
            ReportScope.Company,
            companyId,
            range.From,
            range.To,
            readings,
            null,
            [],
            company,
            departments,
            null);
    }
}
