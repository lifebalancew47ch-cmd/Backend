using Asp.Versioning;
using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.Application.Features.CompanyDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/company")]
[Authorize(Policy = Policies.DashboardRead)]
public class CompanyDashboardController : ApiControllerBase
{
    private const string DefaultCompanyId = "comp_001";

    private string GetTargetCompanyId(string? companyId) => string.IsNullOrWhiteSpace(companyId) ? DefaultCompanyId : companyId;

    [HttpGet]
    public async Task<IActionResult> GetCompanyDashboard([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyDashboardQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyKpisQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyStatisticsQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyDepartmentsQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyHeatmapQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("adherence")]
    public async Task<IActionResult> GetAdherence([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyAdherenceQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyTrendsQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyRankingQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("licenses")]
    public async Task<IActionResult> GetLicenses([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyLicensesQuery(GetTargetCompanyId(companyId)), cancellationToken));

    [HttpGet("organization")]
    public async Task<IActionResult> GetOrganization([FromQuery] string? companyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetCompanyOrganizationQuery(GetTargetCompanyId(companyId)), cancellationToken));
}
