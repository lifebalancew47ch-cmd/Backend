using FluentAssertions;
using LifeBalance.Reporting.Application.Features.ReportExport;
using LifeBalance.Reporting.Application.Features.ReportHistory;
using LifeBalance.Reporting.Application.Features.ReportStatistics;
using LifeBalance.Reporting.Application.Features.ReportTrends;
using LifeBalance.Reporting.Application.Validators;
using LifeBalance.Reporting.Domain.Enums;

namespace LifeBalance.Reporting.UnitTests.Validators;

public class ReportQueryValidatorsTests
{
    [Fact]
    public void ScopedQuery_ValidInput_Passes()
    {
        var validator = new GetReportStatisticsQueryValidator();
        var query = new GetReportStatisticsQuery(
            ReportScope.Company, "comp-1", "user-1", ["USER"],
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 31));

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ScopedQuery_MissingScopeIdForFamily_Fails()
    {
        var validator = new GetReportStatisticsQueryValidator();
        var query = new GetReportStatisticsQuery(ReportScope.Family, null, "user-1", ["USER"], null, null);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ScopeId");
    }

    [Fact]
    public void ScopedQuery_RangeExceedsMaxDays_Fails()
    {
        var validator = new GetReportStatisticsQueryValidator();
        var query = new GetReportStatisticsQuery(
            ReportScope.Individual, null, "user-1", ["USER"],
            new DateTime(2020, 1, 1), new DateTime(2026, 1, 1));

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Trends_UnknownMetric_Fails()
    {
        var validator = new GetReportTrendsQueryValidator();
        var query = new GetReportTrendsQuery(
            ReportScope.Individual, null, "user-1", ["USER"], null, null, ["bogus"]);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Metrics[0]");
    }

    [Fact]
    public void Export_InvalidFormat_Fails()
    {
        var validator = new ExportReportQueryValidator();
        var query = new ExportReportQuery(
            ReportScope.Individual, null, "user-1", ["USER"],
            (ReportFormat)99, null, null, []);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void History_PageSizeTooLarge_Fails()
    {
        var validator = new GetReportHistoryQueryValidator();
        var query = new GetReportHistoryQuery("user-1", 0, 500, null, null);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void History_ValidInput_Passes()
    {
        var validator = new GetReportHistoryQueryValidator();
        var query = new GetReportHistoryQuery("user-1", 0, 20, ReportScope.Individual, ReportFormat.Pdf);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
