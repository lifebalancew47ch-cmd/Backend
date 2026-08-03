using Asp.Versioning;
using LifeBalance.Reporting.API.Authorization;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Features.ReportTrends;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Reporting.API.Controllers;

/// <summary>
/// Trend analysis of historical health metrics for a scope.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/trends")]
[Authorize(Policy = Policies.ReportRead)]
[EnableRateLimiting("fixed")]
public sealed class ReportTrendsController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>Initializes a new instance of <see cref="ReportTrendsController"/>.</summary>
    public ReportTrendsController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    private IActionResult? ResolveIdentity(out string userId, out IReadOnlyList<string> roles)
    {
        userId = _currentUserService.UserId ?? string.Empty;
        roles = _currentUserService.Roles;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<object>.Fail(
                "A valid user identity could not be resolved from the authentication token.",
                statusCode: StatusCodes.Status401Unauthorized,
                traceId: HttpContext.TraceIdentifier));
        }

        return null;
    }

    /// <summary>Computes historical trends (regression + moving average) for the selected scope.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ReportTrendsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrends(
        [FromQuery] ReportScope scope,
        [FromQuery] string? scopeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] IReadOnlyList<string>? metrics,
        CancellationToken cancellationToken)
    {
        var authResult = ResolveIdentity(out var userId, out var roles);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(
            new GetReportTrendsQuery(scope, scopeId, userId, roles, from, to, metrics ?? Array.Empty<string>()),
            cancellationToken));
    }
}
