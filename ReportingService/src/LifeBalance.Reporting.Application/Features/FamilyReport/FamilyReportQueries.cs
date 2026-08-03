using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.FamilyReport;

public sealed record GetFamilyReportQuery(
    string FamilyId,
    string RequesterUserId,
    IReadOnlyList<string> RequesterRoles,
    DateTime? From,
    DateTime? To) : IRequest<Result<FamilyReportResponse>>;

public sealed record FamilyReportResponse(
    string FamilyId,
    string AdministratorUserId,
    IReadOnlyList<FamilyMemberReportDto> Members,
    FamilyComparativesDto Comparatives,
    IReadOnlyList<FamilyMemberRankDto> Ranking,
    FamilyActivitySummaryDto Activity,
    FamilyComplianceSummaryDto Compliance,
    DateTime From,
    DateTime To,
    DateTime GeneratedAtUtc);

public sealed record FamilyMemberReportDto(
    string UserId,
    string FullName,
    double AverageSteps,
    double AverageSedentaryHours,
    double AverageHeartRate,
    int MeasurementDays);

public sealed record FamilyComparativesDto(double AverageSteps, double AverageSedentaryHours, double AverageHeartRate, int MemberCount);

public sealed record FamilyMemberRankDto(int Position, string UserId, string FullName, double AverageSteps);

public sealed record FamilyActivitySummaryDto(double TotalSteps, double AverageStepsPerMember, double ActiveDaysRatio);

public sealed record FamilyComplianceSummaryDto(double CompliancePercentage, int ActiveMembers, int TotalMembers);

/// <summary>
/// Generates the family report: member profiles, comparative averages, rankings,
/// activity and compliance, consolidating data from the Organization, Auth,
/// Medical Data and Sedentary Engine services.
/// </summary>
public sealed class GetFamilyReportQueryHandler : IRequestHandler<GetFamilyReportQuery, Result<FamilyReportResponse>>
{
    private readonly IReportDatasetService _datasetService;
    private readonly ISedentaryEngineServiceClient _sedentaryClient;
    private readonly IStatisticalAnalyzer _analyzer;
    private readonly IDateTimeProvider _dateTime;

    public GetFamilyReportQueryHandler(
        IReportDatasetService datasetService,
        ISedentaryEngineServiceClient sedentaryClient,
        IStatisticalAnalyzer analyzer,
        IDateTimeProvider dateTime)
    {
        _datasetService = datasetService;
        _sedentaryClient = sedentaryClient;
        _analyzer = analyzer;
        _dateTime = dateTime;
    }

    public async Task<Result<FamilyReportResponse>> Handle(
        GetFamilyReportQuery request,
        CancellationToken cancellationToken)
    {
        var range = ReportDateRangeHelper.Resolve(request.From, request.To, _dateTime.UtcNow);

        var dataset = await _datasetService.BuildAsync(
            ReportScope.Family,
            request.FamilyId,
            request.RequesterUserId,
            request.RequesterRoles,
            range,
            cancellationToken);

        var compliance = await _sedentaryClient.GetFamilyComplianceAsync(
                request.FamilyId, range.From, range.To, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"Compliance for family '{request.FamilyId}' is unavailable.");

        var family = dataset.Family!;
        var members = dataset.Members;
        var readingsByMember = dataset.Readings.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var memberReports = members
            .Select(m => BuildMemberReport(m, readingsByMember.TryGetValue(m.UserId, out var list) ? list : []))
            .ToList();

        var comparatives = new FamilyComparativesDto(
            AverageSteps: _analyzer.Mean(memberReports.Select(m => m.AverageSteps)),
            AverageSedentaryHours: _analyzer.Mean(memberReports.Select(m => m.AverageSedentaryHours)),
            AverageHeartRate: _analyzer.Mean(memberReports.Select(m => m.AverageHeartRate)),
            MemberCount: memberReports.Count);

        var ranking = memberReports
            .OrderByDescending(m => m.AverageSteps)
            .Select((m, index) => new FamilyMemberRankDto(index + 1, m.UserId, m.FullName, m.AverageSteps))
            .ToList();

        var activity = new FamilyActivitySummaryDto(
            TotalSteps: memberReports.Sum(m => m.AverageSteps),
            AverageStepsPerMember: memberReports.Count == 0 ? 0 : memberReports.Average(m => m.AverageSteps),
            ActiveDaysRatio: memberReports.Count == 0 ? 0 : memberReports.Count(m => m.MeasurementDays > 0) / (double)memberReports.Count);

        var complianceSummary = new FamilyComplianceSummaryDto(
            CompliancePercentage: compliance.CompliancePercentage,
            ActiveMembers: compliance.ActiveMembers,
            TotalMembers: compliance.TotalMembers);

        return Result.Success(new FamilyReportResponse(
            FamilyId: request.FamilyId,
            AdministratorUserId: family.AdministratorUserId,
            Members: memberReports,
            Comparatives: comparatives,
            Ranking: ranking,
            Activity: activity,
            Compliance: complianceSummary,
            From: range.From,
            To: range.To,
            GeneratedAtUtc: _dateTime.UtcNow));
    }

    private FamilyMemberReportDto BuildMemberReport(AuthUserProfileDto member, IReadOnlyList<MedicalReadingDto> readings)
    {
        var steps = readings.Where(r => r.Steps > 0).Select(r => (double)r.Steps).ToList();
        var heartRate = readings.Where(r => r.HeartRate.HasValue).Select(r => r.HeartRate!.Value).ToList();

        // Sedentary hours are not part of a MedicalReading; a per-member value of zero
        // is reported until the Sedentary Engine exposes per-member daily history.
        return new FamilyMemberReportDto(
            UserId: member.UserId,
            FullName: $"{member.FirstName} {member.LastName}".Trim(),
            AverageSteps: _analyzer.Mean(steps),
            AverageSedentaryHours: 0,
            AverageHeartRate: _analyzer.Mean(heartRate),
            MeasurementDays: readings.Select(r => r.RecordedAtUtc.Date).Distinct().Count());
    }
}
