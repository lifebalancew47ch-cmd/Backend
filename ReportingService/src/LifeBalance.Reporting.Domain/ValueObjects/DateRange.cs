using LifeBalance.Reporting.Domain.Common;
using LifeBalance.Reporting.Domain.Exceptions;

namespace LifeBalance.Reporting.Domain.ValueObjects;

/// <summary>
/// Value object representing an inclusive date range used to filter historical reports.
/// </summary>
public sealed class DateRange : ValueObject
{
    /// <summary>Initializes a new instance of <see cref="DateRange"/>.</summary>
    /// <param name="from">Inclusive start of the range.</param>
    /// <param name="to">Inclusive end of the range.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="from"/> is after <paramref name="to"/>.</exception>
    public DateRange(DateTime from, DateTime to)
    {
        if (from > to)
        {
            throw new DomainException("The start date cannot be after the end date.");
        }

        From = from;
        To = to;
    }

    /// <summary>Gets the inclusive start of the range.</summary>
    public DateTime From { get; }

    /// <summary>Gets the inclusive end of the range.</summary>
    public DateTime To { get; }

    /// <summary>Gets the total number of days spanned by the range.</summary>
    public int TotalDays => (To - From).Days + 1;

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return From;
        yield return To;
    }
}
