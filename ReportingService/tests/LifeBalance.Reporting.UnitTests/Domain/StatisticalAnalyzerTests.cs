using FluentAssertions;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;

namespace LifeBalance.Reporting.UnitTests.Domain;

public class StatisticalAnalyzerTests
{
    private readonly StatisticalAnalyzer _analyzer = new();

    [Fact]
    public void Describe_OnSample_ComputesExpectedStatistics()
    {
        var result = _analyzer.Describe([1, 2, 3, 4, 5]);

        result.Count.Should().Be(5);
        result.Min.Should().Be(1);
        result.Max.Should().Be(5);
        result.Mean.Should().Be(3);
        result.Median.Should().Be(3);
    }

    [Fact]
    public void Describe_OnEmptySample_ReturnsZeros()
    {
        var result = _analyzer.Describe([]);

        result.Count.Should().Be(0);
        result.Mean.Should().Be(0);
        result.StandardDeviation.Should().Be(0);
    }

    [Fact]
    public void Mean_OnSample_ReturnsAverage()
    {
        _analyzer.Mean([2, 4, 6]).Should().Be(4);
    }

    [Fact]
    public void Mean_OnEmptySample_ReturnsZero()
    {
        _analyzer.Mean([]).Should().Be(0);
    }

    [Fact]
    public void Median_OnEvenSample_ReturnsMiddleAverage()
    {
        _analyzer.Median([1, 2, 3, 4]).Should().Be(2.5);
    }

    [Fact]
    public void Median_OnOddSample_ReturnsMiddleValue()
    {
        _analyzer.Median([5, 1, 3]).Should().Be(3);
    }

    [Fact]
    public void Percentile_NearestRank_ReturnsExpectedValue()
    {
        var values = new[] { 1.0, 2.0, 3.0, 4.0 };
        _analyzer.Percentile(values, 75).Should().Be(3);
    }

    [Fact]
    public void DailyAverages_AggregatesByUtcDay()
    {
        var points = new[]
        {
            (new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), 10.0),
            (new DateTime(2026, 1, 1, 20, 0, 0, DateTimeKind.Utc), 20.0),
            (new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc), 30.0)
        };

        var result = _analyzer.DailyAverages(points);

        result.Should().HaveCount(2);
        result[0].Value.Should().Be(15);
        result[1].Value.Should().Be(30);
    }

    [Fact]
    public void WeeklyAverages_AggregatesByIsoWeek()
    {
        var monday = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc); // Monday
        var points = new[]
        {
            (monday, 10.0),
            (monday.AddDays(1), 20.0),
            (monday.AddDays(7), 40.0)
        };

        var result = _analyzer.WeeklyAverages(points);

        result.Should().HaveCount(2);
        result[0].Value.Should().Be(15);
        result[1].Value.Should().Be(40);
    }

    [Fact]
    public void MonthlyAverages_AggregatesByMonth()
    {
        var points = new[]
        {
            (new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 10.0),
            (new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), 20.0),
            (new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), 30.0)
        };

        var result = _analyzer.MonthlyAverages(points);

        result.Should().HaveCount(2);
        result[0].Value.Should().Be(15);
        result[1].Value.Should().Be(30);
    }

    [Fact]
    public void MovingAverage_ComputesTrailingWindow()
    {
        var points = new SeriesPoint[]
        {
            new(new DateTime(2026, 1, 1), 1),
            new(new DateTime(2026, 1, 2), 2),
            new(new DateTime(2026, 1, 3), 3),
            new(new DateTime(2026, 1, 4), 4)
        };

        var result = _analyzer.MovingAverage(points, 3);

        result.Should().HaveCount(4);
        result[0].Value.Should().Be(1);
        result[1].Value.Should().Be(1.5);
        result[2].Value.Should().Be(2);
        result[3].Value.Should().Be(3);
    }

    [Fact]
    public void Trend_OnIncreasingSeries_ReturnsIncreasingDirection()
    {
        var points = Enumerable.Range(1, 10)
            .Select(i => (new DateTime(2026, 1, i), (double)i))
            .ToArray();

        var result = _analyzer.Trend(points);

        result.Direction.Should().Be(TrendDirection.Increasing);
        result.Slope.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Trend_OnDecreasingSeries_ReturnsDecreasingDirection()
    {
        var points = Enumerable.Range(1, 10)
            .Select(i => (new DateTime(2026, 1, i), (double)(11 - i)))
            .ToArray();

        var result = _analyzer.Trend(points);

        result.Direction.Should().Be(TrendDirection.Decreasing);
        result.Slope.Should().BeLessThan(0);
    }

    [Fact]
    public void Trend_OnEmptySeries_ReturnsStable()
    {
        var result = _analyzer.Trend([]);

        result.Direction.Should().Be(TrendDirection.Stable);
    }
}
