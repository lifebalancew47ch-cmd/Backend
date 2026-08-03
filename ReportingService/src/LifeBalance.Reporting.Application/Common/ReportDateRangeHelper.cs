using FluentValidation.Results;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Domain.Constants;
using LifeBalance.Reporting.Domain.ValueObjects;
using ValidationException = LifeBalance.Reporting.Application.Exceptions.ValidationException;

namespace LifeBalance.Reporting.Application.Common;

/// <summary>
/// Resolves and validates the date range of a report.
/// </summary>
public static class ReportDateRangeHelper
{
    /// <summary>
    /// Resolves a nullable date range to concrete UTC dates. When no range is supplied,
    /// the last <see cref="DomainConstants.DefaultReportDays"/> days are used. Ranges longer
    /// than <see cref="DomainConstants.MaxReportDays"/> days are rejected.
    /// </summary>
    public static DateRange Resolve(DateTime? from, DateTime? to, DateTime nowUtc)
    {
        var toUtc = (to ?? nowUtc).ToUniversalTime();
        var fromUtc = (from ?? toUtc.AddDays(-(DomainConstants.DefaultReportDays - 1))).ToUniversalTime();

        if (fromUtc > toUtc)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(DateRange), "The start date cannot be after the end date.")
            });
        }

        if ((toUtc.Date - fromUtc.Date).TotalDays + 1 > DomainConstants.MaxReportDays)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(DateRange),
                    $"The date range cannot exceed {DomainConstants.MaxReportDays} days.")
            });
        }

        return new DateRange(fromUtc.Date, toUtc.Date.AddDays(1).AddTicks(-1));
    }
}
