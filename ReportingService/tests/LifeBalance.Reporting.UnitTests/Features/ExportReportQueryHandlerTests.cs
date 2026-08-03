using FluentAssertions;
using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Features.ReportExport;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.ValueObjects;
using NSubstitute;

namespace LifeBalance.Reporting.UnitTests.Features;

public class ExportReportQueryHandlerTests
{
    private readonly IReportDatasetService _datasetService = Substitute.For<IReportDatasetService>();
    private readonly IStatisticalAnalyzer _analyzer = new StatisticalAnalyzer();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly IPdfReportGenerator _pdf = Substitute.For<IPdfReportGenerator>();
    private readonly IExcelReportGenerator _excel = Substitute.For<IExcelReportGenerator>();
    private readonly ICsvReportGenerator _csv = Substitute.For<ICsvReportGenerator>();
    private readonly IReportGenerationLogService _logService = Substitute.For<IReportGenerationLogService>();
    private readonly ExportReportQueryHandler _handler;

    public ExportReportQueryHandlerTests()
    {
        _dateTime.UtcNow.Returns(new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc));
        _handler = new ExportReportQueryHandler(_datasetService, _analyzer, _dateTime, _pdf, _excel, _csv, _logService);
    }

    private static ReportDataset CreateDataset() =>
        new(
            Scope: ReportScope.Individual,
            ScopeId: "user-1",
            From: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
            Readings:
            [
                new MedicalReadingDto("1", "user-1", null, null, 70, 60, 97, 5000, null, null, null, null, null, null, null, null, 120, 80, 70, 175, null, new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)),
                new MedicalReadingDto("2", "user-1", null, null, 75, 62, 98, 6000, null, null, null, null, null, null, null, null, 122, 81, 70, 175, null, new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc))
            ],
            UserProfile: new AuthUserProfileDto("user-1", "a@b.io", "A", "B", ["USER"], null, null),
            Members: [],
            Company: null,
            Departments: [],
            Family: null);

    [Fact]
    public async Task Handle_Csv_UsesCsvGenerator()
    {
        _datasetService.BuildAsync(Arg.Any<ReportScope>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(CreateDataset());
        _csv.Generate(Arg.Any<ReportExportData>()).Returns(new byte[] { 1, 2, 3 });

        var result = await _handler.Handle(
            new ExportReportQuery(ReportScope.Individual, null, "user-1", ["USER"], ReportFormat.Csv, null, null, ["steps"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("text/csv");
        result.Value.FileName.Should().EndWith(".csv");
        result.Value.Content.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Pdf_UsesPdfGenerator()
    {
        _datasetService.BuildAsync(Arg.Any<ReportScope>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(CreateDataset());
        _pdf.Generate(Arg.Any<ReportExportData>()).Returns(new byte[] { 9, 8 });

        var result = await _handler.Handle(
            new ExportReportQuery(ReportScope.Individual, null, "user-1", ["USER"], ReportFormat.Pdf, null, null, ["heartrate"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Handle_Excel_UsesExcelGenerator()
    {
        _datasetService.BuildAsync(Arg.Any<ReportScope>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(CreateDataset());
        _excel.Generate(Arg.Any<ReportExportData>()).Returns(new byte[] { 4, 5, 6, 7 });

        var result = await _handler.Handle(
            new ExportReportQuery(ReportScope.Individual, null, "user-1", ["USER"], ReportFormat.Excel, null, null, ["steps", "heartrate"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Contain("spreadsheetml");
        result.Value.FileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task Handle_LogsGeneration()
    {
        _datasetService.BuildAsync(Arg.Any<ReportScope>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(CreateDataset());
        _csv.Generate(Arg.Any<ReportExportData>()).Returns(new byte[] { 1 });

        await _handler.Handle(
            new ExportReportQuery(ReportScope.Individual, null, "user-1", ["USER"], ReportFormat.Csv, null, null, ["steps"]),
            CancellationToken.None);

        await _logService.Received(1).LogAsync(
            Arg.Any<ReportScope>(),
            Arg.Any<string?>(),
            Arg.Is("user-1"),
            Arg.Any<ReportFormat?>(),
            Arg.Any<ReportStatus>(),
            Arg.Any<double>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}


