using AutoMapper;
using FluentAssertions;
using LifeBalance.Reporting.Application.Features.ReportHistory;
using LifeBalance.Reporting.Domain.Entities;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.Repositories;
using NSubstitute;

namespace LifeBalance.Reporting.UnitTests.Features;

public class ReportHistoryQueryHandlerTests
{
    private readonly IReportGenerationLogRepository _repository = Substitute.For<IReportGenerationLogRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetReportHistoryQueryHandler _handler;

    public ReportHistoryQueryHandlerTests()
    {
        _handler = new GetReportHistoryQueryHandler(_repository, _mapper);
    }

    [Fact]
    public async Task Handle_ReturnsPaginatedHistory()
    {
        var log = new ReportGenerationLog
        {
            Id = "64b000000000000000000001",
            UserId = "user-1",
            Scope = ReportScope.Individual,
            ScopeId = "user-1",
            Format = ReportFormat.Pdf,
            Status = ReportStatus.Completed,
            DurationMs = 120.5,
            RecordCount = 10,
            TimestampUtc = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc)
        };

        var items = new List<ReportGenerationLog> { log };
        _repository.GetByUserAsync(
                Arg.Is("user-1"), Arg.Is(0), Arg.Is(20), Arg.Any<ReportScope?>(), Arg.Any<ReportFormat?>(), Arg.Any<CancellationToken>())
            .Returns((items, 1));
        _mapper.Map<IReadOnlyList<ReportHistoryItemDto>>(Arg.Any<IReadOnlyList<ReportGenerationLog>>())
            .Returns([
                new ReportHistoryItemDto(
                    Id: log.Id,
                    Scope: log.Scope,
                    ScopeId: log.ScopeId,
                    Format: log.Format,
                    Status: log.Status,
                    DurationMs: log.DurationMs,
                    RecordCount: log.RecordCount,
                    TimestampUtc: log.TimestampUtc)
            ]);

        var result = await _handler.Handle(
            new GetReportHistoryQuery("user-1", 0, 20, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItems.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Scope.Should().Be(ReportScope.Individual);
        result.Value.Items[0].Status.Should().Be(ReportStatus.Completed);
    }

    [Fact]
    public async Task Handle_ClampsPageSizeToMax()
    {
        _repository.GetByUserAsync(
                Arg.Is("user-1"), Arg.Is(0), Arg.Is(100), Arg.Any<ReportScope?>(), Arg.Any<ReportFormat?>(), Arg.Any<CancellationToken>())
            .Returns((new List<ReportGenerationLog>(), 0));
        _mapper.Map<IReadOnlyList<ReportHistoryItemDto>>(Arg.Any<IReadOnlyList<ReportGenerationLog>>())
            .Returns([]);

        var result = await _handler.Handle(
            new GetReportHistoryQuery("user-1", 0, 5000, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(100);
    }
}
