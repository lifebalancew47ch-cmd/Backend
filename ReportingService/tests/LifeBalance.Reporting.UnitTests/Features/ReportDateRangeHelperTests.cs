using FluentAssertions;
using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Domain.Constants;

namespace LifeBalance.Reporting.UnitTests.Features;

public class ReportDateRangeHelperTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Resolve_NoRange_DefaultsToLast30Days()
    {
        var range = ReportDateRangeHelper.Resolve(null, null, NowUtc);

        range.To.Date.Should().Be(NowUtc.Date);
        range.From.Date.Should().Be(NowUtc.Date.AddDays(-(DomainConstants.DefaultReportDays - 1)));
    }

    [Fact]
    public void Resolve_WithRange_ReturnsInclusiveDates()
    {
        var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

        var range = ReportDateRangeHelper.Resolve(from, to, NowUtc);

        range.From.Date.Should().Be(from.Date);
        range.To.Date.Should().Be(to.Date);
        range.TotalDays.Should().Be(31);
    }

    [Fact]
    public void Resolve_FromAfterTo_ThrowsValidationException()
    {
        var from = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        var act = () => ReportDateRangeHelper.Resolve(from, to, NowUtc);

        act.Should().Throw<LifeBalance.Reporting.Application.Exceptions.ValidationException>();
    }

    [Fact]
    public void Resolve_RangeTooLong_ThrowsValidationException()
    {
        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        var act = () => ReportDateRangeHelper.Resolve(from, to, NowUtc);

        act.Should().Throw<LifeBalance.Reporting.Application.Exceptions.ValidationException>();
    }
}
