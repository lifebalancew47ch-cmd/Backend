using LifeBalance.Reporting.Domain.Enums;

namespace LifeBalance.Reporting.Domain.DomainServices;

/// <summary>
/// The output of a linear regression over a time series.
/// </summary>
/// <param name="Slope">The slope of the fitted line (value units per day).</param>
/// <param name="Intercept">The intercept of the fitted line.</param>
/// <param name="RSquared">The coefficient of determination R² (0..1).</param>
/// <param name="Direction">The trend direction derived from the slope.</param>
public sealed record TrendResult(double Slope, double Intercept, double RSquared, TrendDirection Direction);

/// <summary>
/// A single point of a time series.
/// </summary>
/// <param name="Timestamp">The timestamp (typically start of the aggregation bucket).</param>
/// <param name="Value">The aggregated value.</param>
public sealed record SeriesPoint(DateTime Timestamp, double Value);

/// <summary>
/// Descriptive statistics over a sample of values.
/// </summary>
/// <param name="Count">Number of samples.</param>
/// <param name="Min">Minimum value.</param>
/// <param name="Max">Maximum value.</param>
/// <param name="Mean">Arithmetic mean.</param>
/// <param name="Median">Median (50th percentile).</param>
/// <param name="StandardDeviation">Sample standard deviation.</param>
/// <param name="P25">25th percentile.</param>
/// <param name="P75">75th percentile.</param>
/// <param name="P95">95th percentile.</param>
public sealed record DescriptiveStatistics(
    int Count,
    double Min,
    double Max,
    double Mean,
    double Median,
    double StandardDeviation,
    double P25,
    double P75,
    double P95);

/// <summary>
/// Pure statistical and analytical operations used to build historical reports.
/// Implemented as a domain service with no external dependencies, making it fully testable.
/// </summary>
public interface IStatisticalAnalyzer
{
    /// <summary>Computes descriptive statistics (count, mean, median, stddev, percentiles, ...).</summary>
    DescriptiveStatistics Describe(IEnumerable<double> values);

    /// <summary>Computes the arithmetic mean of a sample. Returns 0 for an empty sample.</summary>
    double Mean(IEnumerable<double> values);

    /// <summary>Computes the median (50th percentile). Returns 0 for an empty sample.</summary>
    double Median(IEnumerable<double> values);

    /// <summary>Computes a percentile (0..100) using the nearest-rank method. Returns 0 for an empty sample.</summary>
    double Percentile(IEnumerable<double> values, double percentile);

    /// <summary>Aggregates a series into daily buckets, averaging the values within each UTC day.</summary>
    IReadOnlyList<SeriesPoint> DailyAverages(IEnumerable<(DateTime Timestamp, double Value)> points);

    /// <summary>Aggregates a series into ISO week buckets, averaging the values within each week.</summary>
    IReadOnlyList<SeriesPoint> WeeklyAverages(IEnumerable<(DateTime Timestamp, double Value)> points);

    /// <summary>Aggregates a series into calendar month buckets, averaging the values within each month.</summary>
    IReadOnlyList<SeriesPoint> MonthlyAverages(IEnumerable<(DateTime Timestamp, double Value)> points);

    /// <summary>Computes a centered moving average over a series using the given window size.</summary>
    IReadOnlyList<SeriesPoint> MovingAverage(IEnumerable<SeriesPoint> points, int window);

    /// <summary>Fits a linear regression and returns slope, intercept, R² and trend direction.</summary>
    TrendResult Trend(IEnumerable<(DateTime Timestamp, double Value)> points);
}

/// <summary>
/// Default implementation of <see cref="IStatisticalAnalyzer"/> using pure arithmetic.
/// </summary>
public sealed class StatisticalAnalyzer : IStatisticalAnalyzer
{
    /// <inheritdoc/>
    public DescriptiveStatistics Describe(IEnumerable<double> values)
    {
        var sample = values.Where(double.IsFinite).ToList();

        if (sample.Count == 0)
        {
            return new DescriptiveStatistics(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var mean = sample.Average();
        var variance = sample.Sum(v => Math.Pow(v - mean, 2)) / (sample.Count - 1);

        return new DescriptiveStatistics(
            Count: sample.Count,
            Min: sample.Min(),
            Max: sample.Max(),
            Mean: mean,
            Median: Median(sample),
            StandardDeviation: Math.Sqrt(variance),
            P25: Percentile(sample, 25),
            P75: Percentile(sample, 75),
            P95: Percentile(sample, 95));
    }

    /// <inheritdoc/>
    public double Mean(IEnumerable<double> values)
    {
        var sample = values.Where(double.IsFinite).ToList();
        return sample.Count == 0 ? 0 : sample.Average();
    }

    /// <inheritdoc/>
    public double Median(IEnumerable<double> values)
    {
        var sample = values.Where(double.IsFinite).OrderBy(v => v).ToList();
        if (sample.Count == 0)
        {
            return 0;
        }

        var mid = sample.Count / 2;
        return sample.Count % 2 == 0
            ? (sample[mid - 1] + sample[mid]) / 2.0
            : sample[mid];
    }

    /// <inheritdoc/>
    public double Percentile(IEnumerable<double> values, double percentile)
    {
        var sample = values.Where(double.IsFinite).OrderBy(v => v).ToList();
        if (sample.Count == 0)
        {
            return 0;
        }

        var rank = Math.Max(1, (int)Math.Ceiling(percentile / 100.0 * sample.Count));
        return sample[Math.Min(rank, sample.Count) - 1];
    }

    /// <inheritdoc/>
    public IReadOnlyList<SeriesPoint> DailyAverages(IEnumerable<(DateTime Timestamp, double Value)> points)
        => Aggregate(points, bucketSelector: ts => ts.Date, sortableKey: ts => ts.Date.Ticks);

    /// <inheritdoc/>
    public IReadOnlyList<SeriesPoint> WeeklyAverages(IEnumerable<(DateTime Timestamp, double Value)> points)
        => Aggregate(points, bucketSelector: StartOfIsoWeek, sortableKey: ts => StartOfIsoWeek(ts).Ticks);

    /// <inheritdoc/>
    public IReadOnlyList<SeriesPoint> MonthlyAverages(IEnumerable<(DateTime Timestamp, double Value)> points)
        => Aggregate(points, bucketSelector: ts => new DateTime(ts.Year, ts.Month, 1), sortableKey: ts => new DateTime(ts.Year, ts.Month, 1).Ticks);

    /// <inheritdoc/>
    public IReadOnlyList<SeriesPoint> MovingAverage(IEnumerable<SeriesPoint> points, int window)
    {
        if (window <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "The moving average window must be greater than zero.");
        }

        var ordered = points.OrderBy(p => p.Timestamp).ToList();
        if (ordered.Count == 0)
        {
            return [];
        }

        var result = new List<SeriesPoint>(ordered.Count);
        var cumulative = 0.0;
        var count = 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            cumulative += ordered[i].Value;
            count++;

            if (i >= window)
            {
                cumulative -= ordered[i - window].Value;
                count--;
            }

            result.Add(new SeriesPoint(ordered[i].Timestamp, cumulative / count));
        }

        return result;
    }

    /// <inheritdoc/>
    public TrendResult Trend(IEnumerable<(DateTime Timestamp, double Value)> points)
    {
        var ordered = points
            .Where(p => double.IsFinite(p.Value))
            .OrderBy(p => p.Timestamp)
            .ToList();

        if (ordered.Count < 2)
        {
            return new TrendResult(0, 0, 0, TrendDirection.Stable);
        }

        // Use the day offset from the first sample as the independent variable.
        var origin = ordered[0].Timestamp.Date;
        var xs = ordered.Select(p => (p.Timestamp.Date - origin).TotalDays).ToArray();
        var ys = ordered.Select(p => p.Value).ToArray();

        var n = xs.Length;
        var sumX = xs.Sum();
        var sumY = ys.Sum();
        var sumXx = xs.Sum(x => x * x);
        var sumXy = xs.Zip(ys, (x, y) => x * y).Sum();

        var denominator = n * sumXx - sumX * sumX;
        if (Math.Abs(denominator) < double.Epsilon)
        {
            return new TrendResult(0, sumY / n, 0, TrendDirection.Stable);
        }

        var slope = (n * sumXy - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;

        var meanY = sumY / n;
        var ssTotal = ys.Sum(y => Math.Pow(y - meanY, 2));
        var ssResidual = xs.Zip(ys, (x, y) => Math.Pow(y - (intercept + slope * x), 2)).Sum();

        var rSquared = ssTotal < double.Epsilon ? 0 : 1 - ssResidual / ssTotal;
        rSquared = Math.Clamp(rSquared, 0, 1);

        var direction = slope switch
        {
            > 0.05 => TrendDirection.Increasing,
            < -0.05 => TrendDirection.Decreasing,
            _ => TrendDirection.Stable
        };

        return new TrendResult(slope, intercept, rSquared, direction);
    }

    private static IReadOnlyList<SeriesPoint> Aggregate(
        IEnumerable<(DateTime Timestamp, double Value)> points,
        Func<DateTime, DateTime> bucketSelector,
        Func<DateTime, long> sortableKey)
    {
        return points
            .Where(p => double.IsFinite(p.Value))
            .GroupBy(p => bucketSelector(p.Timestamp))
            .Select(g => new SeriesPoint(g.Key, g.Average(p => p.Value)))
            .OrderBy(p => sortableKey(p.Timestamp))
            .ToList();
    }

    private static DateTime StartOfIsoWeek(DateTime timestamp)
    {
        var dayOfWeek = (int)timestamp.DayOfWeek;
        if (dayOfWeek == 0)
        {
            dayOfWeek = 7;
        }

        return timestamp.Date.AddDays(1 - dayOfWeek);
    }
}
