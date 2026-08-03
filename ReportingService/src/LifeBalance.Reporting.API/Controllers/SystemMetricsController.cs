using Asp.Versioning;
using LifeBalance.Reporting.API.Authorization;
using LifeBalance.Reporting.Application.Features.SystemMetrics;
using LifeBalance.Reporting.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Reporting.API.Controllers;

/// <summary>
/// Platform-wide system metrics consumed by the Dashboard microservice.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/system-metrics")]
[Authorize(Policy = Policies.Admin)]
[EnableRateLimiting("fixed")]
public sealed class SystemMetricsController : ApiControllerBase
{
    /// <summary>Initializes a new instance of <see cref="SystemMetricsController"/>.</summary>
    public SystemMetricsController()
    {
    }

    /// <summary>Returns aggregated platform health, user counts and system version.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GeneralSystemMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemMetrics(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetSystemMetricsQuery(), cancellationToken));
}
