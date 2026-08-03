using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.ReportTrends;

public sealed record GetReportTrendsQuery(
    ReportScope Scope,
    string? ScopeId,
    string RequesterUserId,
    IReadOnlyList<string> RequesterRoles,
    DateTime? From,
    DateTime? To,
    IReadOnlyList<string> Metrics) : IRequest<Result<ReportTrendsResponse>>, IReportScopeQuery;

public sealed record ReportTrendsResponse(
    ReportScope Scope,
    string ScopeId,
    DateTime From,
    DateTime To,
    IReadOnlyList<MetricTrendDto> Trends);

public sealed record MetricTrendDto(
    string Metric,
    string DisplayName,
    TrendResult Trend,
    IReadOnlyList<SeriesPoint> Series,
    IReadOnlyList<SeriesPoint> MovingAverage);

/// <summary>
/// Computes historical trends (linear regression, R², direction and a 7-day moving
/// average) for the requested metrics of a given scope.
/// </summary>
public sealed class GetReportTrendsQueryHandler : IRequestHandler<GetReportTrendsQuery, Result<ReportTrendsResponse>>
{
    private const int MovingAverageWindow = 7;

    private readonly IReportDatasetService _datasetService;
    private readonly IStatisticalAnalyzer _analyzer;
    private readonly IDateTimeProvider _dateTime;

    public GetReportTrendsQueryHandler(
        IReportDatasetService datasetService,
        IStatisticalAnalyzer analyzer,
        IDateTimeProvider dateTime)
    {
        _datasetService = datasetService;
        _analyzer = analyzer;
        _dateTime = dateTime;
    }

    public async Task<Result<ReportTrendsResponse>> Handle(
        GetReportTrendsQuery request,
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

        var metrics = ReportMetrics.Resolve(request.Metrics);
        var trends = new List<MetricTrendDto>(metrics.Count);

        foreach (var metric in metrics)
        {
            var dailyPoints = _analyzer.DailyAverages(dataset.Readings
                .Where(r => metric.Extractor(r).HasValue)
                .Select(r => (Timestamp: r.RecordedAtUtc, Value: metric.Extractor(r)!.Value)));

            trends.Add(new MetricTrendDto(
                metric.Code,
                metric.DisplayName,
                _analyzer.Trend(dailyPoints.Select(p => (Timestamp: p.Timestamp, Value: p.Value))),
                dailyPoints,
                _analyzer.MovingAverage(dailyPoints, MovingAverageWindow)));
        }

        return Result.Success(new ReportTrendsResponse(
            request.Scope,
            dataset.ScopeId,
            range.From,
            range.To,
            trends));
    }
}
