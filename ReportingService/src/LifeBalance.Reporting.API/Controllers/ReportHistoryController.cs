using Asp.Versioning;
using LifeBalance.Reporting.API.Authorization;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Features.ReportHistory;
using LifeBalance.Reporting.Contracts.Common;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Reporting.API.Controllers;

/// <summary>
/// Report generation history of the authenticated user.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/history")]
[Authorize(Policy = Policies.AuthenticatedUser)]
[EnableRateLimiting("fixed")]
public sealed class ReportHistoryController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>Initializes a new instance of <see cref="ReportHistoryController"/>.</summary>
    public ReportHistoryController(ICurrentUserService currentUserService)
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

    /// <summary>Returns the paginated report generation history of the requesting user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ReportHistoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = Shared.Constants.SharedConstants.DefaultPageSize,
        [FromQuery] ReportScope? scope = null,
        [FromQuery] ReportFormat? format = null,
        CancellationToken cancellationToken = default)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(
            new GetReportHistoryQuery(userId, pageIndex, pageSize, scope, format),
            cancellationToken));
    }
}
