using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.DashboardSummary;

public sealed record GetDashboardSummaryQuery(
    ReportScope Scope,
    string? ScopeId,
    string RequesterUserId,
    IReadOnlyList<string> RequesterRoles,
    DateTime? From,
    DateTime? To) : IRequest<Result<DashboardSummaryResponse>>, IReportScopeQuery;

public sealed record DashboardSummaryResponse(
    ReportScope Scope,
    string ScopeId,
    int MeasurementDays,
    int TotalReadings,
    double AverageSteps,
    double AverageHeartRate,
    double AverageHrv,
    double AverageSpo2,
    double AverageSedentaryHours,
    string DominantTrend,
    DateTime GeneratedAtUtc);

/// <summary>
/// Produces a compact summary of the historical indicators of a scope, intended to be
/// consumed by the Dashboard microservice and internal dashboards.
/// </summary>
public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryResponse>>
{
    private readonly IReportDatasetService _datasetService;
    private readonly IStatisticalAnalyzer _analyzer;
    private readonly IDateTimeProvider _dateTime;

    public GetDashboardSummaryQueryHandler(
        IReportDatasetService datasetService,
        IStatisticalAnalyzer analyzer,
        IDateTimeProvider dateTime)
    {
        _datasetService = datasetService;
        _analyzer = analyzer;
        _dateTime = dateTime;
    }

    public async Task<Result<DashboardSummaryResponse>> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var range = ReportDateRangeHelper.Resolve(request.From, request.To, _dateTime.UtcNow);

        var dataset = await _datasetService.BuildAsync(
            request.Scope,
            request.ScopeId,
            request.RequesterUserId,
            request.RequesterRoles,
            range,
            cancellationToken);

        var readings = dataset.Readings;

        var steps = readings.Where(r => r.Steps > 0).Select(r => (double)r.Steps).ToList();
        var heartRate = readings.Where(r => r.HeartRate.HasValue).Select(r => r.HeartRate!.Value).ToList();
        var hrv = readings.Where(r => r.Hrv.HasValue).Select(r => r.Hrv!.Value).ToList();
        var spo2 = readings.Where(r => r.Spo2.HasValue).Select(r => r.Spo2!.Value).ToList();

        var stepsTrend = _analyzer.Trend(readings
            .Where(r => r.Steps > 0)
            .Select(r => (Timestamp: r.RecordedAtUtc, Value: (double)r.Steps)));

        var dominantTrend = stepsTrend.Direction switch
        {
            TrendDirection.Increasing => "increasing",
            TrendDirection.Decreasing => "decreasing",
            _ => "stable"
        };

        return Result.Success(new DashboardSummaryResponse(
            Scope: request.Scope,
            ScopeId: dataset.ScopeId,
            MeasurementDays: readings.Select(r => r.RecordedAtUtc.Date).Distinct().Count(),
            TotalReadings: readings.Count,
            AverageSteps: _analyzer.Mean(steps),
            AverageHeartRate: _analyzer.Mean(heartRate),
            AverageHrv: _analyzer.Mean(hrv),
            AverageSpo2: _analyzer.Mean(spo2),
            AverageSedentaryHours: 0,
            DominantTrend: dominantTrend,
            GeneratedAtUtc: _dateTime.UtcNow));
    }
}
