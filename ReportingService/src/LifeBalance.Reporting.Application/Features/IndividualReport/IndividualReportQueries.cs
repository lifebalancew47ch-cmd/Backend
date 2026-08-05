using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.IndividualReport;

public sealed record GetIndividualReportQuery(
    string UserId,
    DateTime? From,
    DateTime? To) : IRequest<Result<IndividualReportResponse>>;

public sealed record IndividualReportResponse(
    string UserId,
    string FullName,
    DateTime From,
    DateTime To,
    DateTime GeneratedAtUtc,
    VitalSignsSummaryDto VitalSigns,
    ActivitySummaryDto Activity,
    SedentarySummaryDto Sedentary,
    GoalsSummaryDto Goals,
    TrendsSummaryDto Trends,
    IReadOnlyList<DailyMetricDto> DailyAverages);

public sealed record VitalSignsSummaryDto(
    MetricSummaryDto HeartRate,
    MetricSummaryDto Hrv,
    MetricSummaryDto Spo2,
    MetricSummaryDto SystolicBp,
    MetricSummaryDto DiastolicBp,
    MetricSummaryDto Weight,
    MetricSummaryDto Height);

public sealed record MetricSummaryDto(string Name, double Latest, DescriptiveStatistics Statistics);

public sealed record ActivitySummaryDto(
    int TotalSteps,
    double AverageDailySteps,
    double AverageActiveMinutes,
    double AverageCaloriesBurned,
    int MeasurementDays);

public sealed record SedentarySummaryDto(double AverageSedentaryHours, double AverageSedentaryScore, int AverageBreakCount);

public sealed record GoalsSummaryDto(
    int TotalGoals,
    int CompletedGoals,
    double CompliancePercentage,
    IReadOnlyList<GoalSummaryItemDto> Items);

public sealed record GoalSummaryItemDto(string Id, string Name, string Metric, double Target, double Current, bool Completed);

public sealed record TrendsSummaryDto(TrendResult StepsTrend, TrendResult HeartRateTrend);

public sealed record DailyMetricDto(
    DateTime Date,
    double Steps,
    double HeartRate,
    double SedentaryHours,
    double SedentaryScore);

/// <summary>
/// Generates the complete individual report consolidating user profile, vital signs,
/// activity, sedentary behavior, goals and trends from the upstream services.
/// </summary>
public sealed class GetIndividualReportQueryHandler : IRequestHandler<GetIndividualReportQuery, Result<IndividualReportResponse>>
{
    private readonly IReportDatasetService _datasetService;
    private readonly ISedentaryEngineServiceClient _sedentaryClient;
    private readonly IStatisticalAnalyzer _analyzer;
    private readonly IDateTimeProvider _dateTime;

    public GetIndividualReportQueryHandler(
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

    public async Task<Result<IndividualReportResponse>> Handle(
        GetIndividualReportQuery request,
        CancellationToken cancellationToken)
    {
        var range = ReportDateRangeHelper.Resolve(request.From, request.To, _dateTime.UtcNow);

        var dataset = await _datasetService.BuildAsync(
            ReportScope.Individual, null, request.UserId, [], range, cancellationToken);

        var sedentaryHistory = await _sedentaryClient.GetUserHistoryAsync(
                request.UserId, range.From, range.To, cancellationToken) ?? [];
        var goals = await _sedentaryClient.GetUserGoalsAsync(request.UserId, cancellationToken) ?? [];

        var readings = dataset.Readings;
        var profile = dataset.UserProfile!;

        var vitalSigns = new VitalSignsSummaryDto(
            BuildMetric("Heart Rate", readings, r => r.HeartRate),
            BuildMetric("HRV", readings, r => r.Hrv),
            BuildMetric("SpO2", readings, r => r.Spo2),
            BuildMetric("Systolic BP", readings, r => r.SystolicBp),
            BuildMetric("Diastolic BP", readings, r => r.DiastolicBp),
            BuildMetric("Weight", readings, r => r.Weight),
            BuildMetric("Height", readings, r => r.Height));

        var steps = readings.Select(r => (double)r.Steps).ToList();
        var activeMinutes = sedentaryHistory.Select(s => (double)s.ActiveMinutes).ToList();
        var calories = sedentaryHistory.Select(s => 0.0).ToList();

        var activity = new ActivitySummaryDto(
            TotalSteps: (int)steps.Sum(),
            AverageDailySteps: _analyzer.Mean(steps),
            AverageActiveMinutes: _analyzer.Mean(activeMinutes),
            AverageCaloriesBurned: _analyzer.Mean(calories),
            MeasurementDays: readings.Select(r => r.RecordedAtUtc.Date).Distinct().Count());

        var sedentary = new SedentarySummaryDto(
            AverageSedentaryHours: _analyzer.Mean(sedentaryHistory.Select(s => s.SedentaryHours)),
            AverageSedentaryScore: _analyzer.Mean(sedentaryHistory.Select(s => s.SedentaryScore)),
            AverageBreakCount: (int)_analyzer.Mean(sedentaryHistory.Select(s => (double)s.BreakCount)));

        var goalsSummary = new GoalsSummaryDto(
            TotalGoals: goals.Count,
            CompletedGoals: goals.Count(g => g.Completed),
            CompliancePercentage: goals.Count == 0 ? 0 : 100.0 * goals.Count(g => g.Completed) / goals.Count,
            Items: goals
                .Select(g => new GoalSummaryItemDto(g.Id, g.Name, g.Metric, g.Target, g.Current, g.Completed))
                .ToList());

        var stepsPoints = readings
            .Where(r => r.Steps > 0)
            .Select(r => (Timestamp: r.RecordedAtUtc, Value: (double)r.Steps));
        var heartRatePoints = readings
            .Where(r => r.HeartRate.HasValue)
            .Select(r => (Timestamp: r.RecordedAtUtc, Value: r.HeartRate!.Value));

        var trends = new TrendsSummaryDto(
            _analyzer.Trend(stepsPoints),
            _analyzer.Trend(heartRatePoints));

        var dailyAverages = BuildDailyAverages(readings, sedentaryHistory);

        return Result.Success(new IndividualReportResponse(
            UserId: request.UserId,
            FullName: $"{profile.FirstName} {profile.LastName}".Trim(),
            From: range.From,
            To: range.To,
            GeneratedAtUtc: _dateTime.UtcNow,
            VitalSigns: vitalSigns,
            Activity: activity,
            Sedentary: sedentary,
            Goals: goalsSummary,
            Trends: trends,
            DailyAverages: dailyAverages));
    }

    private MetricSummaryDto BuildMetric(string name, IReadOnlyList<MedicalReadingDto> readings, Func<MedicalReadingDto, double?> selector)
    {
        var values = readings.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var latest = values.Count == 0 ? 0 : values[^1];
        return new MetricSummaryDto(name, latest, _analyzer.Describe(values));
    }

    private IReadOnlyList<DailyMetricDto> BuildDailyAverages(
        IReadOnlyList<MedicalReadingDto> readings,
        IReadOnlyList<SedentaryDailyDto> sedentaryHistory)
    {
        var dailySteps = _analyzer.DailyAverages(readings
                .Where(r => r.Steps > 0)
                .Select(r => (Timestamp: r.RecordedAtUtc, Value: (double)r.Steps)))
            .ToDictionary(p => p.Timestamp.Date, p => p.Value);

        var dailyHeartRate = _analyzer.DailyAverages(readings
                .Where(r => r.HeartRate.HasValue)
                .Select(r => (Timestamp: r.RecordedAtUtc, Value: r.HeartRate!.Value)))
            .ToDictionary(p => p.Timestamp.Date, p => p.Value);

        var dailySedentaryHours = sedentaryHistory
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => g.Average(s => s.SedentaryHours));

        var dailySedentaryScore = sedentaryHistory
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => g.Average(s => s.SedentaryScore));

        var dates = dailySteps.Keys
            .Union(dailyHeartRate.Keys)
            .Union(dailySedentaryHours.Keys)
            .OrderBy(d => d)
            .ToList();

        return dates
            .Select(d => new DailyMetricDto(
                d,
                dailySteps.GetValueOrDefault(d),
                dailyHeartRate.GetValueOrDefault(d),
                dailySedentaryHours.GetValueOrDefault(d),
                dailySedentaryScore.GetValueOrDefault(d)))
            .ToList();
    }
}
