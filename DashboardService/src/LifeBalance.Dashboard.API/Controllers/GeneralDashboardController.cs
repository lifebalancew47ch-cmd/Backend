using Asp.Versioning;
using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.Application.Features.GeneralDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[EnableRateLimiting("fixed")]
public class GeneralDashboardController : ApiControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = Policies.DashboardRead)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetGeneralSummaryQuery(), cancellationToken));

    [HttpGet("indicators")]
    [Authorize(Policy = Policies.DashboardRead)]
    public async Task<IActionResult> GetIndicators(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetGeneralIndicatorsQuery(), cancellationToken));

    [HttpGet("kpis")]
    [Authorize(Policy = Policies.DashboardRead)]
    public async Task<IActionResult> GetKpis(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetGeneralKpisQuery(), cancellationToken));

    [HttpGet("system")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSystem(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetGeneralSystemQuery(), cancellationToken));

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetGeneralHealthQuery(), cancellationToken));

    [HttpGet("version")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVersion(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetGeneralVersionQuery(), cancellationToken));
}
