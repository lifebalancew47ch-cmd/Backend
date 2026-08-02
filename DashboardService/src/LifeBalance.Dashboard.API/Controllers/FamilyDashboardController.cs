using Asp.Versioning;
using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.FamilyDashboard;
using LifeBalance.Dashboard.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/family")]
[Authorize(Policy = Policies.DashboardRead)]
[EnableRateLimiting("fixed")]
public class FamilyDashboardController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationServiceClient _orgClient;

    public FamilyDashboardController(ICurrentUserService currentUserService, IOrganizationServiceClient orgClient)
    {
        _currentUserService = currentUserService;
        _orgClient = orgClient;
    }

    private async Task<IActionResult?> ResolveFamilyAccessAsync(string familyId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<object>.Fail(
                "A valid user identity could not be resolved from the authentication token.",
                statusCode: StatusCodes.Status401Unauthorized,
                traceId: HttpContext.TraceIdentifier));
        }

        var family = await _orgClient.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail(
                "The Organization service could not confirm the family membership.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                traceId: HttpContext.TraceIdentifier));
        }

        var isMember = string.Equals(family.AdministratorUserId, userId, StringComparison.OrdinalIgnoreCase)
            || family.MemberUserIds.Contains(userId, StringComparer.OrdinalIgnoreCase);
        if (!isMember)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(
                "The authenticated user is not a member of the requested family.",
                statusCode: StatusCodes.Status403Forbidden,
                traceId: HttpContext.TraceIdentifier));
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetFamilyDashboard([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyDashboardQuery(familyId), cancellationToken));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyStatisticsQuery(familyId), cancellationToken));
    }

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyGoalsQuery(familyId), cancellationToken));
    }

    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyRankingQuery(familyId), cancellationToken));
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyMembersQuery(familyId), cancellationToken));
    }

    [HttpGet("challenges")]
    public async Task<IActionResult> GetChallenges([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyChallengesQuery(familyId), cancellationToken));
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> GetRewards([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyRewardsQuery(familyId), cancellationToken));
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] string? familyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(familyId))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The 'familyId' query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest,
                traceId: HttpContext.TraceIdentifier));
        }

        var accessResult = await ResolveFamilyAccessAsync(familyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return HandleResult(await Mediator.Send(new GetFamilyHeatmapQuery(familyId), cancellationToken));
    }
}
