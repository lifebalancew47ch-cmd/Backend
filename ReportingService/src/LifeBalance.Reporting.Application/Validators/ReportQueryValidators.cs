using FluentValidation;
using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Features.CompanyReport;
using LifeBalance.Reporting.Application.Features.DashboardSummary;
using LifeBalance.Reporting.Application.Features.FamilyReport;
using LifeBalance.Reporting.Application.Features.IndividualReport;
using LifeBalance.Reporting.Application.Features.ReportExport;
using LifeBalance.Reporting.Application.Features.ReportHistory;
using LifeBalance.Reporting.Application.Features.ReportStatistics;
using LifeBalance.Reporting.Application.Features.ReportTrends;
using LifeBalance.Reporting.Domain.Constants;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Constants;

namespace LifeBalance.Reporting.Application.Validators;

public class GetIndividualReportQueryValidator : AbstractValidator<GetIndividualReportQuery>
{
    public GetIndividualReportQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}

public class GetFamilyReportQueryValidator : AbstractValidator<GetFamilyReportQuery>
{
    public GetFamilyReportQueryValidator()
    {
        RuleFor(x => x.FamilyId).NotEmpty().WithMessage("FamilyId is required.");
    }
}

public class GetCompanyReportQueryValidator : AbstractValidator<GetCompanyReportQuery>
{
    public GetCompanyReportQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required.");
    }
}

/// <summary>
/// Base validator for scoped report queries: validates the scope enum, the scope
/// identifier (required for family/company) and the date range.
/// </summary>
/// <typeparam name="TRequest">The scoped report query type.</typeparam>
public abstract class ScopedReportQueryValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : IReportScopeQuery
{
    /// <summary>Initializes a new instance of <see cref="ScopedReportQueryValidator{TRequest}"/>.</summary>
    protected ScopedReportQueryValidator()
    {
        RuleFor(x => x.Scope).IsInEnum().WithMessage("Scope must be a valid report scope.");

        RuleFor(x => x.ScopeId)
            .NotEmpty()
            .When(x => x.Scope != ReportScope.Individual)
            .WithMessage("ScopeId is required for family and company reports.");

        RuleFor(x => x.From)
            .Must((request, from) => IsValidRange(from, request.To))
            .WithMessage(
                $"Invalid date range: 'from' must not be after 'to' and the range cannot exceed {DomainConstants.MaxReportDays} days.");
    }

    private static bool IsValidRange(DateTime? from, DateTime? to)
    {
        if (!from.HasValue || !to.HasValue)
        {
            return true;
        }

        if (from.Value > to.Value)
        {
            return false;
        }

        return (to.Value.Date - from.Value.Date).TotalDays <= DomainConstants.MaxReportDays - 1;
    }
}

public class GetReportStatisticsQueryValidator : ScopedReportQueryValidator<GetReportStatisticsQuery>
{
}

public class GetReportTrendsQueryValidator : ScopedReportQueryValidator<GetReportTrendsQuery>
{
    public GetReportTrendsQueryValidator()
    {
        RuleForEach(x => x.Metrics)
            .Must(metric => ReportMetrics.TryGet(metric, out _))
            .WithMessage("Unknown metric code '{PropertyValue}'.");
    }
}

public class GetDashboardSummaryQueryValidator : ScopedReportQueryValidator<GetDashboardSummaryQuery>
{
}

public class ExportReportQueryValidator : ScopedReportQueryValidator<ExportReportQuery>
{
    public ExportReportQueryValidator()
    {
        RuleFor(x => x.Format).IsInEnum().WithMessage("Format must be a valid report format.");

        RuleForEach(x => x.Metrics)
            .Must(metric => ReportMetrics.TryGet(metric, out _))
            .WithMessage("Unknown metric code '{PropertyValue}'.");
    }
}

public class GetReportHistoryQueryValidator : AbstractValidator<GetReportHistoryQuery>
{
    public GetReportHistoryQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(0).WithMessage("PageIndex must be zero or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, SharedConstants.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {SharedConstants.MaxPageSize}.");

        RuleFor(x => x.Scope).IsInEnum().WithMessage("Scope must be a valid report scope.");

        RuleFor(x => x.Format).IsInEnum().WithMessage("Format must be a valid report format.");
    }
}
