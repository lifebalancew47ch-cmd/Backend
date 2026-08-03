using FluentAssertions;
using LifeBalance.Reporting.Application.Common;

namespace LifeBalance.Reporting.UnitTests.Features;

public class ReportMetricsTests
{
    [Fact]
    public void Resolve_NullCodes_ReturnsAllMetrics()
    {
        var result = ReportMetrics.Resolve(null);

        result.Should().HaveCount(ReportMetrics.All.Count);
    }

    [Fact]
    public void Resolve_EmptyCodes_ReturnsAllMetrics()
    {
        var result = ReportMetrics.Resolve([]);

        result.Should().HaveCount(ReportMetrics.All.Count);
    }

    [Fact]
    public void Resolve_KnownCodes_ReturnsMatchingMetrics()
    {
        var result = ReportMetrics.Resolve(["steps", "heartrate"]);

        result.Should().HaveCount(2);
        result.Select(m => m.Code).Should().Contain(["steps", "heartrate"]);
    }

    [Fact]
    public void Resolve_UnknownCodes_ReturnsAllMetrics()
    {
        var result = ReportMetrics.Resolve(["nonexistent"]);

        result.Should().HaveCount(ReportMetrics.All.Count);
    }

    [Fact]
    public void TryGet_KnownCode_ReturnsMetric()
    {
        var found = ReportMetrics.TryGet("spo2", out var metric);

        found.Should().BeTrue();
        metric.DisplayName.Should().Be("SpO2");
    }

    [Fact]
    public void TryGet_UnknownCode_ReturnsFalse()
    {
        var found = ReportMetrics.TryGet("bogus", out _);

        found.Should().BeFalse();
    }
}
