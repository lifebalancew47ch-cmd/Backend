using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.ReportStatistics;

public sealed record GetReportStatisticsQuery(
    ReportScope Scope,
    string? ScopeId,
    string RequesterUserId,
    IReadOnlyList<string> RequesterRoles,
    DateTime? From,
    DateTime? To) : IRequest<Result<ReportStatisticsResponse>>, IReportScopeQuery;

public sealed record ReportStatisticsResponse(
    ReportScope Scope,
    string ScopeId,
    DateTime From,
    DateTime To,
    IReadOnlyList<MetricStatisticsDto> Metrics,
    IReadOnlyList<MetricSeriesDto> Daily,
    IReadOnlyList<MetricSeriesDto> Weekly,
    IReadOnlyList<MetricSeriesDto> Monthly);

public sealed record MetricStatisticsDto(string Metric, string DisplayName, DescriptiveStatistics Statistics);

public sealed record MetricSeriesDto(string Metric, string DisplayName, IReadOnlyList<SeriesPoint> Points);

/// <summary>
/// Computes historical statistics (daily/weekly/monthly averages and descriptive
/// statistics) for the supported metrics of a given scope.
/// </summary>
public sealed class GetReportStatisticsQueryHandler : IRequestHandler<GetReportStatisticsQuery, Result<ReportStatisticsResponse>>
{
    private readonly IReportDatasetService _datasetService;
    private readonly IStatisticalAnalyzer _analyzer;
    private readonly IDateTimeProvider _dateTime;

    public GetReportStatisticsQueryHandler(
        IReportDatasetService datasetService,
        IStatisticalAnalyzer analyzer,
        IDateTimeProvider dateTime)
    {
        _datasetService = datasetService;
        _analyzer = analyzer;
        _dateTime = dateTime;
    }

    public async Task<Result<ReportStatisticsResponse>> Handle(
        GetReportStatisticsQuery request,
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

        var metrics = new List<MetricStatisticsDto>();
        var daily = new List<MetricSeriesDto>();
        var weekly = new List<MetricSeriesDto>();
        var monthly = new List<MetricSeriesDto>();

        foreach (var metric in ReportMetrics.All)
        {
            var points = dataset.Readings
                .Where(r => metric.Extractor(r).HasValue)
                .Select(r => (Timestamp: r.RecordedAtUtc, Value: metric.Extractor(r)!.Value))
                .ToList();

            var statistics = _analyzer.Describe(points.Select(p => p.Value));

            metrics.Add(new MetricStatisticsDto(metric.Code, metric.DisplayName, statistics));
            daily.Add(new MetricSeriesDto(metric.Code, metric.DisplayName, _analyzer.DailyAverages(points)));
            weekly.Add(new MetricSeriesDto(metric.Code, metric.DisplayName, _analyzer.WeeklyAverages(points)));
            monthly.Add(new MetricSeriesDto(metric.Code, metric.DisplayName, _analyzer.MonthlyAverages(points)));
        }

        return Result.Success(new ReportStatisticsResponse(
            request.Scope,
            dataset.ScopeId,
            range.From,
            range.To,
            metrics,
            daily,
            weekly,
            monthly));
    }
}
