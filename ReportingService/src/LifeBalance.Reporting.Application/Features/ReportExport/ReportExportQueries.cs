using System.Globalization;
using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.ReportExport;

public sealed record ExportReportQuery(
    ReportScope Scope,
    string? ScopeId,
    string RequesterUserId,
    IReadOnlyList<string> RequesterRoles,
    ReportFormat Format,
    DateTime? From,
    DateTime? To,
    IReadOnlyList<string> Metrics) : IRequest<Result<ReportExportResult>>, IReportScopeQuery;

/// <summary>
/// Generates a downloadable report document (PDF, Excel or CSV) for a scope.
/// The data is consolidated from upstream services and rendered server-side.
/// </summary>
public sealed class ExportReportQueryHandler : IRequestHandler<ExportReportQuery, Result<ReportExportResult>>
{
    private readonly IReportDatasetService _datasetService;
    private readonly IStatisticalAnalyzer _analyzer;
    private readonly IDateTimeProvider _dateTime;
    private readonly IPdfReportGenerator _pdfGenerator;
    private readonly IExcelReportGenerator _excelGenerator;
    private readonly ICsvReportGenerator _csvGenerator;
    private readonly IReportGenerationLogService _logService;

    public ExportReportQueryHandler(
        IReportDatasetService datasetService,
        IStatisticalAnalyzer analyzer,
        IDateTimeProvider dateTime,
        IPdfReportGenerator pdfGenerator,
        IExcelReportGenerator excelGenerator,
        ICsvReportGenerator csvGenerator,
        IReportGenerationLogService logService)
    {
        _datasetService = datasetService;
        _analyzer = analyzer;
        _dateTime = dateTime;
        _pdfGenerator = pdfGenerator;
        _excelGenerator = excelGenerator;
        _csvGenerator = csvGenerator;
        _logService = logService;
    }

    public async Task<Result<ReportExportResult>> Handle(
        ExportReportQuery request,
        CancellationToken cancellationToken)
    {
        var range = ReportDateRangeHelper.Resolve(request.From, request.To, _dateTime.UtcNow);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var dataset = await _datasetService.BuildAsync(
                request.Scope,
                request.ScopeId,
                request.RequesterUserId,
                request.RequesterRoles,
                range,
                cancellationToken);

            var metrics = ReportMetrics.Resolve(request.Metrics);
            var data = BuildExportData(request.Scope, dataset, metrics, range);

            var (fileName, contentType, content) = request.Format switch
            {
                ReportFormat.Pdf => ("pdf", "application/pdf", _pdfGenerator.Generate(data)),
                ReportFormat.Excel => ("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _excelGenerator.Generate(data)),
                ReportFormat.Csv => ("csv", "text/csv", _csvGenerator.Generate(data)),
                _ => throw new ArgumentOutOfRangeException(nameof(request.Format))
            };

            stopwatch.Stop();

            var result = new ReportExportResult(
                FileName: BuildFileName(request.Scope, dataset.ScopeId, range.To, fileName),
                ContentType: contentType,
                Content: content);

            await _logService.LogAsync(
                request.Scope,
                dataset.ScopeId,
                request.RequesterUserId,
                request.Format,
                ReportStatus.Completed,
                stopwatch.Elapsed.TotalMilliseconds,
                data.Rows.Count,
                correlationId: null,
                cancellationToken: cancellationToken);

            return Result.Success(result);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            throw;
        }
    }

    private ReportExportData BuildExportData(
        ReportScope scope,
        ReportDataset dataset,
        IReadOnlyList<ReportMetricDefinition> metrics,
        Domain.ValueObjects.DateRange range)
    {
        var columns = new List<string> { "Date" };
        columns.AddRange(metrics.Select(m => m.DisplayName));

        var series = metrics
            .ToDictionary(
                m => m.Code,
                m => _analyzer.DailyAverages(dataset.Readings
                    .Where(r => m.Extractor(r).HasValue)
                    .Select(r => (Timestamp: r.RecordedAtUtc, Value: m.Extractor(r)!.Value)))
                    .ToDictionary(p => p.Timestamp.Date, p => p.Value));

        var dates = series.Values
            .SelectMany(s => s.Keys)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var rows = dates
            .Select(date =>
            {
                var cells = new List<string> { date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
                cells.AddRange(metrics.Select(m => series[m.Code].GetValueOrDefault(date).ToString("0.##", CultureInfo.InvariantCulture)));
                return (IReadOnlyList<string>)cells;
            })
            .ToList();

        return new ReportExportData(
            Title: $"LifeBalance {scope} Report",
            Subtitle: $"Scope: {dataset.ScopeId} | Period: {range.From:yyyy-MM-dd} to {range.To:yyyy-MM-dd}",
            GeneratedAtUtc: _dateTime.UtcNow,
            Columns: columns,
            Rows: rows);
    }

    private static string BuildFileName(ReportScope scope, string scopeId, DateTime to, string extension)
    {
        var safeScope = Shared.Helpers.StringHelper.ToSafeFileName(scope.ToString().ToLowerInvariant());
        var safeId = Shared.Helpers.StringHelper.ToSafeFileName(scopeId);
        return $"{safeScope}_{safeId}_{to:yyyyMMdd}.{extension}";
    }
}
