using Asp.Versioning;
using LifeBalance.Reporting.API.Authorization;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Features.ReportExport;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Reporting.API.Controllers;

/// <summary>
/// Downloads report documents (PDF, Excel, CSV) generated server-side.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/export")]
[Authorize(Policy = Policies.ReportExport)]
[EnableRateLimiting("fixed")]
public sealed class ReportExportController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>Initializes a new instance of <see cref="ReportExportController"/>.</summary>
    public ReportExportController(ICurrentUserService currentUserService)
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

    /// <summary>Generates and downloads a report document for the selected scope.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] ReportScope scope,
        [FromQuery] string? scopeId,
        [FromQuery] ReportFormat format,
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

        var result = await Mediator.Send(
            new ExportReportQuery(scope, scopeId, userId, roles, format, from, to, metrics ?? Array.Empty<string>()),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest();
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }
}
