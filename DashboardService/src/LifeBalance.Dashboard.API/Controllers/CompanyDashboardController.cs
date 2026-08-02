using Asp.Versioning;
using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.CompanyDashboard;
using LifeBalance.Dashboard.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/company")]
[Authorize(Policy = Policies.DashboardRead)]
[EnableRateLimiting("fixed")]
public class CompanyDashboardController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationServiceClient _orgClient;

    public CompanyDashboardController(ICurrentUserService currentUserService, IOrganizationServiceClient orgClient)
    {
        _currentUserService = currentUserService;
        _orgClient = orgClient;
    }

    private async Task<IActionResult?> ResolveCompanyAccessAsync(string companyId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<object>.Fail(
                "A valid user identity could not be resolved from the authentication token.",
                statusCode: StatusCodes.Status401Unauthorized,
                traceId: HttpContext.TraceIdentifier));
        }

        var departments = await _orgClient.GetCompanyDepartmentsWithMembersAsync(companyId, cancellationToken);
        if (departments is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail(
                "The Organization service could not confirm the company membership.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                traceId: HttpContext.TraceIdentifier));
        }

        var isMember = departments.Any(d => d.MemberUserIds.Contains(userId, StringComparer.OrdinalIgnoreCase));
        if (!isMember)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(
                "The authenticated user is not a member of the requested company.",
                statusCode: StatusCodes.Status403Forbidden,
                traceId: HttpContext.TraceIdentifier));
        }

        return null;
    }

    private IActionResult? RequireCompanyId(string? companyId)
    {
        if (!string.IsNullOrWhiteSpace(companyId))
        {
            return null;
        }

        return BadRequest(ApiResponse<object>.Fail(
            "The 'companyId' query parameter is required.",
            statusCode: StatusCodes.Status400BadRequest,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanyDashboard([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyDashboardQuery(companyId!), cancellationToken));
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyKpisQuery(companyId!), cancellationToken));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyStatisticsQuery(companyId!), cancellationToken));
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyDepartmentsQuery(companyId!), cancellationToken));
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyHeatmapQuery(companyId!), cancellationToken));
    }

    [HttpGet("adherence")]
    public async Task<IActionResult> GetAdherence([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyAdherenceQuery(companyId!), cancellationToken));
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyTrendsQuery(companyId!), cancellationToken));
    }

    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyRankingQuery(companyId!), cancellationToken));
    }

    [HttpGet("licenses")]
    public async Task<IActionResult> GetLicenses([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyLicensesQuery(companyId!), cancellationToken));
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetOrganization([FromQuery] string? companyId, CancellationToken cancellationToken)
    {
        var validationResult = RequireCompanyId(companyId);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var accessResult = await ResolveCompanyAccessAsync(companyId!, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetCompanyOrganizationQuery(companyId!), cancellationToken));
    }
}
