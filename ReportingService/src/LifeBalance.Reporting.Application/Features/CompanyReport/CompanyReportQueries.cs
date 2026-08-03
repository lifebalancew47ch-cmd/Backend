using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.CompanyReport;

public sealed record GetCompanyReportQuery(
    string CompanyId,
    string RequesterUserId,
    IReadOnlyList<string> RequesterRoles,
    DateTime? From,
    DateTime? To) : IRequest<Result<CompanyReportResponse>>;

public sealed record CompanyReportResponse(
    string CompanyId,
    string CompanyName,
    IReadOnlyList<DepartmentReportDto> Departments,
    CompanyIndicatorsDto Indicators,
    CompanyComplianceSummaryDto Compliance,
    CompanyAdherenceSummaryDto Adherence,
    CompanyAnonymizedStatisticsDto Anonymized,
    DateTime From,
    DateTime To,
    DateTime GeneratedAtUtc);

public sealed record DepartmentReportDto(
    string DepartmentId,
    string DepartmentName,
    int TotalMembers,
    double AverageSteps,
    double AverageSedentaryHours,
    double AverageHeartRate,
    double AdherencePercentage);

public sealed record CompanyIndicatorsDto(
    int TotalEmployees,
    double AverageSteps,
    double AverageSedentaryHours,
    double AverageHeartRate,
    double AverageWeight,
    int MeasurementDays);

public sealed record CompanyComplianceSummaryDto(double CompliancePercentage, int ActiveEmployees, int TotalEmployees);

public sealed record CompanyAdherenceSummaryDto(
    double AdherencePercentage,
    int TotalEmployees,
    int ActiveEmployees,
    IReadOnlyList<string> HighRiskDepartments);

public sealed record CompanyAnonymizedStatisticsDto(
    double MedianSteps,
    double P25Steps,
    double P75Steps,
    double AverageHeartRate,
    double MedianSedentaryHours);

/// <summary>
/// Generates the company report: department indicators, averages, compliance,
/// adherence and anonymized statistics, consolidating data from the Organization,
/// Medical Data and Sedentary Engine services.
/// </summary>
public sealed class GetCompanyReportQueryHandler : IRequestHandler<GetCompanyReportQuery, Result<CompanyReportResponse>>
{
    private readonly IReportDatasetService _datasetService;
    private readonly ISedentaryEngineServiceClient _sedentaryClient;
    private readonly IStatisticalAnalyzer _analyzer;
    private readonly IDateTimeProvider _dateTime;

    public GetCompanyReportQueryHandler(
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

    public async Task<Result<CompanyReportResponse>> Handle(
        GetCompanyReportQuery request,
        CancellationToken cancellationToken)
    {
        var range = ReportDateRangeHelper.Resolve(request.From, request.To, _dateTime.UtcNow);

        var dataset = await _datasetService.BuildAsync(
            ReportScope.Company,
            request.CompanyId,
            request.RequesterUserId,
            request.RequesterRoles,
            range,
            cancellationToken);

        var adherence = await _sedentaryClient.GetCompanyAdherenceAsync(
                request.CompanyId, range.From, range.To, cancellationToken)
            ?? throw new UpstreamServiceUnavailableException(
                $"Adherence for company '{request.CompanyId}' is unavailable.");

        var company = dataset.Company!;
        var departments = dataset.Departments;
        var readings = dataset.Readings;

        var departmentReports = departments
            .Select(d => BuildDepartmentReport(d, readings))
            .ToList();

        var allSteps = readings.Where(r => r.Steps > 0).Select(r => (double)r.Steps).ToList();
        var allHeartRate = readings.Where(r => r.HeartRate.HasValue).Select(r => r.HeartRate!.Value).ToList();
        var allWeight = readings.Where(r => r.Weight.HasValue).Select(r => r.Weight!.Value).ToList();

        var indicators = new CompanyIndicatorsDto(
            TotalEmployees: company.TotalEmployees,
            AverageSteps: _analyzer.Mean(allSteps),
            AverageSedentaryHours: 0,
            AverageHeartRate: _analyzer.Mean(allHeartRate),
            AverageWeight: _analyzer.Mean(allWeight),
            MeasurementDays: readings.Select(r => r.RecordedAtUtc.Date).Distinct().Count());

        var compliance = new CompanyComplianceSummaryDto(
            CompliancePercentage: adherence.AdherencePercentage,
            ActiveEmployees: adherence.ActiveEmployees,
            TotalEmployees: adherence.TotalEmployees);

        var adherenceSummary = new CompanyAdherenceSummaryDto(
            AdherencePercentage: adherence.AdherencePercentage,
            TotalEmployees: adherence.TotalEmployees,
            ActiveEmployees: adherence.ActiveEmployees,
            HighRiskDepartments: adherence.HighRiskDepartments);

        var anonymized = new CompanyAnonymizedStatisticsDto(
            MedianSteps: _analyzer.Median(allSteps),
            P25Steps: _analyzer.Percentile(allSteps, 25),
            P75Steps: _analyzer.Percentile(allSteps, 75),
            AverageHeartRate: _analyzer.Mean(allHeartRate),
            MedianSedentaryHours: 0);

        return Result.Success(new CompanyReportResponse(
            CompanyId: request.CompanyId,
            CompanyName: company.Name,
            Departments: departmentReports,
            Indicators: indicators,
            Compliance: compliance,
            Adherence: adherenceSummary,
            Anonymized: anonymized,
            From: range.From,
            To: range.To,
            GeneratedAtUtc: _dateTime.UtcNow));
    }

    private DepartmentReportDto BuildDepartmentReport(
        CompanyDepartmentMembersDto department,
        IReadOnlyList<MedicalReadingDto> readings)
    {
        var memberSet = department.MemberUserIds.ToHashSet();
        var departmentReadings = readings.Where(r => memberSet.Contains(r.UserId)).ToList();

        var steps = departmentReadings.Where(r => r.Steps > 0).Select(r => (double)r.Steps).ToList();
        var heartRate = departmentReadings.Where(r => r.HeartRate.HasValue).Select(r => r.HeartRate!.Value).ToList();

        return new DepartmentReportDto(
            DepartmentId: department.DepartmentId,
            DepartmentName: department.DepartmentName,
            TotalMembers: department.MemberUserIds.Count,
            AverageSteps: _analyzer.Mean(steps),
            AverageSedentaryHours: 0,
            AverageHeartRate: _analyzer.Mean(heartRate),
            AdherencePercentage: 0);
    }
}
