using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.CompanyDashboard;
using NSubstitute;
using Xunit;

namespace LifeBalance.Dashboard.UnitTests.Features;

public class CompanyDashboardQueryHandlersTests
{
    private readonly ISedentaryEngineServiceClient _sedentaryClient = Substitute.For<ISedentaryEngineServiceClient>();
    private readonly IOrganizationServiceClient _orgClient = Substitute.For<IOrganizationServiceClient>();

    private readonly CompanyDashboardQueryHandlers _handler;

    public CompanyDashboardQueryHandlersTests()
    {
        _handler = new CompanyDashboardQueryHandlers(_sedentaryClient, _orgClient);
    }

    private static List<DepartmentSummaryDto> CreateDepartments(string companyId) =>
        new()
        {
            new DepartmentSummaryDto("d1", "Sales", 40, 92.0),
            new DepartmentSummaryDto("d2", "Engineering", 60, 78.5)
        };

    // ── GET /api/v1/dashboard/company ──

    [Fact]
    public async Task Handle_GetCompanyDashboardQuery_ReturnsSuccessfulResult()
    {
        var companyId = "comp_test_001";
        _sedentaryClient.GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyAdherenceResponseDto(companyId, 92.0, 100, 92, new List<string>()));

        var result = await _handler.Handle(new GetCompanyDashboardQuery(companyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(companyId);
        result.Value.Adherence!.AdherencePercentage.Should().Be(92.0);
    }

    [Fact]
    public async Task Handle_GetCompanyDashboardQuery_AllDownstreamNull_ReturnsEmptyDepartments()
    {
        var result = await _handler.Handle(new GetCompanyDashboardQuery("comp_empty"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Departments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetCompanyDashboardQuery_AggregatesThreeSources()
    {
        var companyId = "comp_test_001";
        _sedentaryClient.GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyAdherenceResponseDto(companyId, 92.0, 100, 92, new List<string>()));
        _orgClient.GetCompanyLicensesAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyLicenseDto(companyId, 200, 150, DateTime.UtcNow.AddYears(1), "Enterprise"));
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(CreateDepartments(companyId));

        await _handler.Handle(new GetCompanyDashboardQuery(companyId), CancellationToken.None);

        await _sedentaryClient.Received(1).GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>());
        await _orgClient.Received(1).GetCompanyLicensesAsync(companyId, Arg.Any<CancellationToken>());
        await _orgClient.Received(1).GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetCompanyDashboardQuery_PreservesDepartmentsAndLicenses()
    {
        var companyId = "comp_test_001";
        var departments = CreateDepartments(companyId);
        var licenses = new CompanyLicenseDto(companyId, 500, 250, DateTime.UtcNow.AddMonths(6), "Business");
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(departments);
        _orgClient.GetCompanyLicensesAsync(companyId, Arg.Any<CancellationToken>()).Returns(licenses);

        var result = await _handler.Handle(new GetCompanyDashboardQuery(companyId), CancellationToken.None);

        result.Value.Departments.Should().BeEquivalentTo(departments);
        result.Value.Licenses.Should().Be(licenses);
    }

    [Fact]
    public async Task Handle_GetCompanyDashboardQuery_ReturnsCompanyIdEcho()
    {
        var result = await _handler.Handle(new GetCompanyDashboardQuery("comp_echo"), CancellationToken.None);

        result.Value.CompanyId.Should().Be("comp_echo");
    }

    // ── GET /api/v1/dashboard/company/kpis ──

    [Fact]
    public async Task Handle_GetCompanyKpisQuery_ReturnsAdherenceKpis()
    {
        var companyId = "comp_test_001";
        _sedentaryClient.GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyAdherenceResponseDto(companyId, 88.0, 120, 100, new List<string> { "Sales" }));

        var result = await _handler.Handle(new GetCompanyKpisQuery(companyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdherencePercentage.Should().Be(88.0);
        result.Value.TotalEmployees.Should().Be(120);
        result.Value.HighRiskCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_GetCompanyKpisQuery_NullAdherence_UsesDefaults()
    {
        var result = await _handler.Handle(new GetCompanyKpisQuery("comp_ghost"), CancellationToken.None);

        result.Value.AdherencePercentage.Should().Be(82.5);
        result.Value.TotalEmployees.Should().Be(150);
        result.Value.HighRiskCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_GetCompanyKpisQuery_CountsHighRiskDepartments()
    {
        var companyId = "comp_test_001";
        _sedentaryClient.GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyAdherenceResponseDto(companyId, 70.0, 100, 70, new List<string> { "Sales", "Ops", "QA" }));

        var result = await _handler.Handle(new GetCompanyKpisQuery(companyId), CancellationToken.None);

        result.Value.HighRiskCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_GetCompanyKpisQuery_CallsSedentaryClient()
    {
        var companyId = "comp_test_001";
        await _handler.Handle(new GetCompanyKpisQuery(companyId), CancellationToken.None);

        await _sedentaryClient.Received(1).GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetCompanyKpisQuery_ReturnsCompanyIdEcho()
    {
        var result = await _handler.Handle(new GetCompanyKpisQuery("comp_kpi"), CancellationToken.None);

        result.Value.CompanyId.Should().Be("comp_kpi");
    }

    // ── GET /api/v1/dashboard/company/statistics ──

    [Fact]
    public async Task Handle_GetCompanyStatisticsQuery_ReturnsFixedValues()
    {
        var result = await _handler.Handle(new GetCompanyStatisticsQuery("comp_test_001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSedentaryHours.Should().Be(1250.0);
        result.Value.TotalActiveMinutes.Should().Be(3400.0);
    }

    [Fact]
    public async Task Handle_GetCompanyStatisticsQuery_DoesNotCallDownstream()
    {
        await _handler.Handle(new GetCompanyStatisticsQuery("comp_test_001"), CancellationToken.None);

        await _sedentaryClient.DidNotReceiveWithAnyArgs().GetCompanyAdherenceAsync(default, default);
        await _orgClient.DidNotReceiveWithAnyArgs().GetDepartmentsAsync(default, default);
    }

    [Fact]
    public async Task Handle_GetCompanyStatisticsQuery_StableAcrossCalls()
    {
        var first = await _handler.Handle(new GetCompanyStatisticsQuery("comp_1"), CancellationToken.None);
        var second = await _handler.Handle(new GetCompanyStatisticsQuery("comp_1"), CancellationToken.None);

        first.Value.Should().BeEquivalentTo(second.Value);
    }

    [Fact]
    public async Task Handle_GetCompanyStatisticsQuery_SucceedsForAnyCompany()
    {
        var result = await _handler.Handle(new GetCompanyStatisticsQuery("comp_unknown"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GetCompanyStatisticsQuery_ReturnsCompanyIdEcho()
    {
        var result = await _handler.Handle(new GetCompanyStatisticsQuery("comp_stat"), CancellationToken.None);

        result.Value.CompanyId.Should().Be("comp_stat");
    }

    // ── GET /api/v1/dashboard/company/departments ──

    [Fact]
    public async Task Handle_GetCompanyDepartmentsQuery_ReturnsDepartments()
    {
        var companyId = "comp_test_001";
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(CreateDepartments(companyId));

        var result = await _handler.Handle(new GetCompanyDepartmentsQuery(companyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Departments.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_GetCompanyDepartmentsQuery_NullDepartments_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetCompanyDepartmentsQuery("comp_empty"), CancellationToken.None);

        result.Value.Departments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetCompanyDepartmentsQuery_PreservesDepartmentData()
    {
        var companyId = "comp_test_001";
        var departments = CreateDepartments(companyId);
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(departments);

        var result = await _handler.Handle(new GetCompanyDepartmentsQuery(companyId), CancellationToken.None);

        result.Value.Departments.Should().BeEquivalentTo(departments);
    }

    [Fact]
    public async Task Handle_GetCompanyDepartmentsQuery_CallsOrgClient()
    {
        var companyId = "comp_test_001";
        await _handler.Handle(new GetCompanyDepartmentsQuery(companyId), CancellationToken.None);

        await _orgClient.Received(1).GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetCompanyDepartmentsQuery_ReturnsCompanyIdEcho()
    {
        var result = await _handler.Handle(new GetCompanyDepartmentsQuery("comp_dept"), CancellationToken.None);

        result.Value.CompanyId.Should().Be("comp_dept");
    }

    // ── GET /api/v1/dashboard/company/heatmap ──

    [Fact]
    public async Task Handle_GetCompanyHeatmapQuery_Returns24EntryHeatmap()
    {
        var result = await _handler.Handle(new GetCompanyHeatmapQuery("comp_test_001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DepartmentHeatmap.Should().HaveCount(24);
    }

    [Fact]
    public async Task Handle_GetCompanyHeatmapQuery_AllEntriesFixedAtTwentyFive()
    {
        var result = await _handler.Handle(new GetCompanyHeatmapQuery("comp_test_001"), CancellationToken.None);

        result.Value.DepartmentHeatmap.Should().OnlyContain(v => v == 25);
    }

    [Fact]
    public async Task Handle_GetCompanyHeatmapQuery_DoesNotCallDownstream()
    {
        await _handler.Handle(new GetCompanyHeatmapQuery("comp_test_001"), CancellationToken.None);

        await _orgClient.DidNotReceiveWithAnyArgs().GetDepartmentsAsync(default, default);
        await _sedentaryClient.DidNotReceiveWithAnyArgs().GetCompanyAdherenceAsync(default, default);
    }

    [Fact]
    public async Task Handle_GetCompanyHeatmapQuery_StableAcrossCalls()
    {
        var first = await _handler.Handle(new GetCompanyHeatmapQuery("comp_1"), CancellationToken.None);
        var second = await _handler.Handle(new GetCompanyHeatmapQuery("comp_1"), CancellationToken.None);

        first.Value.Should().BeEquivalentTo(second.Value);
    }

    [Fact]
    public async Task Handle_GetCompanyHeatmapQuery_ReturnsCompanyIdEcho()
    {
        var result = await _handler.Handle(new GetCompanyHeatmapQuery("comp_heat"), CancellationToken.None);

        result.Value.CompanyId.Should().Be("comp_heat");
    }

    // ── GET /api/v1/dashboard/company/adherence ──

    [Fact]
    public async Task Handle_GetCompanyAdherenceQuery_ReturnsAdherence()
    {
        var companyId = "comp_test_001";
        var adherence = new CompanyAdherenceResponseDto(companyId, 91.0, 150, 135, new List<string>());
        _sedentaryClient.GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>()).Returns(adherence);

        var result = await _handler.Handle(new GetCompanyAdherenceQuery(companyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Adherence.Should().Be(adherence);
    }

    [Fact]
    public async Task Handle_GetCompanyAdherenceQuery_NullAdherence_UsesFallback()
    {
        var result = await _handler.Handle(new GetCompanyAdherenceQuery("comp_ghost"), CancellationToken.None);

        result.Value.Adherence.AdherencePercentage.Should().Be(85.0);
        result.Value.Adherence.TotalEmployees.Should().Be(100);
        result.Value.Adherence.ActiveEmployees.Should().Be(85);
    }

    [Fact]
    public async Task Handle_GetCompanyAdherenceQuery_FallbackHasHighRiskDepartments()
    {
        var result = await _handler.Handle(new GetCompanyAdherenceQuery("comp_ghost"), CancellationToken.None);

        result.Value.Adherence.HighRiskDepartments.Should().Contain("Sales");
    }

    [Fact]
    public async Task Handle_GetCompanyAdherenceQuery_PropagatesHighRiskList()
    {
        var companyId = "comp_test_001";
        _sedentaryClient.GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyAdherenceResponseDto(companyId, 60.0, 200, 120, new List<string> { "QA", "Support" }));

        var result = await _handler.Handle(new GetCompanyAdherenceQuery(companyId), CancellationToken.None);

        result.Value.Adherence.HighRiskDepartments.Should().BeEquivalentTo(new[] { "QA", "Support" });
    }

    [Fact]
    public async Task Handle_GetCompanyAdherenceQuery_CallsSedentaryClient()
    {
        var companyId = "comp_test_001";
        await _handler.Handle(new GetCompanyAdherenceQuery(companyId), CancellationToken.None);

        await _sedentaryClient.Received(1).GetCompanyAdherenceAsync(companyId, Arg.Any<CancellationToken>());
    }

    // ── GET /api/v1/dashboard/company/trends ──

    [Fact]
    public async Task Handle_GetCompanyTrendsQuery_ReturnsFivePointTrend()
    {
        var result = await _handler.Handle(new GetCompanyTrendsQuery("comp_test_001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MonthlyAdherenceTrend.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_GetCompanyTrendsQuery_TrendIsMonotonicallyIncreasing()
    {
        var result = await _handler.Handle(new GetCompanyTrendsQuery("comp_test_001"), CancellationToken.None);

        result.Value.MonthlyAdherenceTrend.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_GetCompanyTrendsQuery_FixedValues()
    {
        var result = await _handler.Handle(new GetCompanyTrendsQuery("comp_test_001"), CancellationToken.None);

        result.Value.MonthlyAdherenceTrend.Should().BeEquivalentTo(new List<double> { 70.0, 75.0, 80.0, 85.0, 88.0 });
    }

    [Fact]
    public async Task Handle_GetCompanyTrendsQuery_DoesNotCallDownstream()
    {
        await _handler.Handle(new GetCompanyTrendsQuery("comp_test_001"), CancellationToken.None);

        await _orgClient.DidNotReceiveWithAnyArgs().GetDepartmentsAsync(default, default);
    }

    [Fact]
    public async Task Handle_GetCompanyTrendsQuery_ReturnsCompanyIdEcho()
    {
        var result = await _handler.Handle(new GetCompanyTrendsQuery("comp_trend"), CancellationToken.None);

        result.Value.CompanyId.Should().Be("comp_trend");
    }

    // ── GET /api/v1/dashboard/company/ranking ──

    [Fact]
    public async Task Handle_GetCompanyRankingQuery_RanksDepartments()
    {
        var companyId = "comp_test_001";
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(CreateDepartments(companyId));

        var result = await _handler.Handle(new GetCompanyRankingQuery(companyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DepartmentRankings.Should().HaveCount(2);
        result.Value.DepartmentRankings[0].Rank.Should().Be(1);
        result.Value.DepartmentRankings[1].Rank.Should().Be(2);
    }

    [Fact]
    public async Task Handle_GetCompanyRankingQuery_UsesAdherenceScore()
    {
        var companyId = "comp_test_001";
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(CreateDepartments(companyId));

        var result = await _handler.Handle(new GetCompanyRankingQuery(companyId), CancellationToken.None);

        result.Value.DepartmentRankings[0].Score.Should().Be(92.0);
        result.Value.DepartmentRankings[0].DepartmentName.Should().Be("Sales");
    }

    [Fact]
    public async Task Handle_GetCompanyRankingQuery_NullDepartments_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetCompanyRankingQuery("comp_empty"), CancellationToken.None);

        result.Value.DepartmentRankings.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetCompanyRankingQuery_CallsOrgClient()
    {
        var companyId = "comp_test_001";
        await _handler.Handle(new GetCompanyRankingQuery(companyId), CancellationToken.None);

        await _orgClient.Received(1).GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GetCompanyRankingQuery_ReturnsCompanyIdEcho()
    {
        var result = await _handler.Handle(new GetCompanyRankingQuery("comp_rank"), CancellationToken.None);

        result.Value.CompanyId.Should().Be("comp_rank");
    }

    // ── GET /api/v1/dashboard/company/licenses ──

    [Fact]
    public async Task Handle_GetCompanyLicensesQuery_ReturnsLicenses()
    {
        var companyId = "comp_test_001";
        var licenses = new CompanyLicenseDto(companyId, 300, 210, DateTime.UtcNow.AddYears(1), "Business");
        _orgClient.GetCompanyLicensesAsync(companyId, Arg.Any<CancellationToken>()).Returns(licenses);

        var result = await _handler.Handle(new GetCompanyLicensesQuery(companyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Licenses.Should().Be(licenses);
    }

    [Fact]
    public async Task Handle_GetCompanyLicensesQuery_NullLicenses_UsesFallback()
    {
        var result = await _handler.Handle(new GetCompanyLicensesQuery("comp_ghost"), CancellationToken.None);

        result.Value.Licenses.TotalLicenses.Should().Be(200);
        result.Value.Licenses.UsedLicenses.Should().Be(150);
        result.Value.Licenses.PlanType.Should().Be("Enterprise");
    }

    [Fact]
    public async Task Handle_GetCompanyLicensesQuery_FallbackExpiryInFuture()
    {
        var result = await _handler.Handle(new GetCompanyLicensesQuery("comp_ghost"), CancellationToken.None);

        result.Value.Licenses.ExpirationDateUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_GetCompanyLicensesQuery_PropagatesPlanType()
    {
        var companyId = "comp_test_001";
        _orgClient.GetCompanyLicensesAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new CompanyLicenseDto(companyId, 50, 10, DateTime.UtcNow.AddMonths(2), "Free"));

        var result = await _handler.Handle(new GetCompanyLicensesQuery(companyId), CancellationToken.None);

        result.Value.Licenses.PlanType.Should().Be("Free");
    }

    [Fact]
    public async Task Handle_GetCompanyLicensesQuery_CallsOrgClient()
    {
        var companyId = "comp_test_001";
        await _handler.Handle(new GetCompanyLicensesQuery(companyId), CancellationToken.None);

        await _orgClient.Received(1).GetCompanyLicensesAsync(companyId, Arg.Any<CancellationToken>());
    }

    // ── GET /api/v1/dashboard/company/organization ──

    [Fact]
    public async Task Handle_GetCompanyOrganizationQuery_SummarizesDepartments()
    {
        var companyId = "comp_test_001";
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(CreateDepartments(companyId));

        var result = await _handler.Handle(new GetCompanyOrganizationQuery(companyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDepartments.Should().Be(2);
        result.Value.TotalEmployees.Should().Be(100);
    }

    [Fact]
    public async Task Handle_GetCompanyOrganizationQuery_ReturnsDepartmentNames()
    {
        var companyId = "comp_test_001";
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(CreateDepartments(companyId));

        var result = await _handler.Handle(new GetCompanyOrganizationQuery(companyId), CancellationToken.None);

        result.Value.DepartmentNames.Should().BeEquivalentTo(new[] { "Sales", "Engineering" });
    }

    [Fact]
    public async Task Handle_GetCompanyOrganizationQuery_NullDepartments_ReturnsZeros()
    {
        var result = await _handler.Handle(new GetCompanyOrganizationQuery("comp_empty"), CancellationToken.None);

        result.Value.TotalDepartments.Should().Be(0);
        result.Value.TotalEmployees.Should().Be(0);
        result.Value.DepartmentNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetCompanyOrganizationQuery_SumsMemberTotals()
    {
        var companyId = "comp_test_001";
        var departments = new List<DepartmentSummaryDto>
        {
            new("d1", "A", 10, 80.0),
            new("d2", "B", 20, 90.0),
            new("d3", "C", 30, 70.0)
        };
        _orgClient.GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>()).Returns(departments);

        var result = await _handler.Handle(new GetCompanyOrganizationQuery(companyId), CancellationToken.None);

        result.Value.TotalDepartments.Should().Be(3);
        result.Value.TotalEmployees.Should().Be(60);
    }

    [Fact]
    public async Task Handle_GetCompanyOrganizationQuery_CallsOrgClient()
    {
        var companyId = "comp_test_001";
        await _handler.Handle(new GetCompanyOrganizationQuery(companyId), CancellationToken.None);

        await _orgClient.Received(1).GetDepartmentsAsync(companyId, Arg.Any<CancellationToken>());
    }
}
