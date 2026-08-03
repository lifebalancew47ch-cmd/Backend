using Asp.Versioning;
using LifeBalance.Reporting.API.Authorization;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Features.CompanyReport;
using LifeBalance.Reporting.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Reporting.API.Controllers;

/// <summary>
/// Endpoints that produce a comprehensive historical report for a company.
/// Membership is validated to prevent unauthorized (IDOR) access.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/company")]
[Authorize(Policy = Policies.ReportRead)]
[EnableRateLimiting("fixed")]
public sealed class CompanyReportsController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>Initializes a new instance of <see cref="CompanyReportsController"/>.</summary>
    public CompanyReportsController(ICurrentUserService currentUserService)
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

    /// <summary>Generates a comprehensive company historical report.</summary>
    [HttpGet("{companyId}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanyReport(
        string companyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var authResult = ResolveIdentity(out var userId, out var roles);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(
            new GetCompanyReportQuery(companyId, userId, roles, from, to),
            cancellationToken));
    }
}
