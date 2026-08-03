using Asp.Versioning;
using LifeBalance.Reporting.API.Authorization;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Features.IndividualReport;
using LifeBalance.Reporting.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Reporting.API.Controllers;

/// <summary>
/// Endpoints that produce a comprehensive historical report for an individual user.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/individual")]
[Authorize(Policy = Policies.ReportRead)]
[EnableRateLimiting("fixed")]
public sealed class IndividualReportsController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>Initializes a new instance of <see cref="IndividualReportsController"/>.</summary>
    public IndividualReportsController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    private IActionResult? ResolveUserId(out string userId)
    {
        userId = _currentUserService.UserId ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return Unauthorized(ApiResponse<object>.Fail(
            "A valid user identity could not be resolved from the authentication token.",
            statusCode: StatusCodes.Status401Unauthorized,
            traceId: HttpContext.TraceIdentifier));
    }

    /// <summary>Generates a comprehensive individual historical report.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IndividualReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIndividualReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(
            new GetIndividualReportQuery(userId, from, to),
            cancellationToken));
    }
}
