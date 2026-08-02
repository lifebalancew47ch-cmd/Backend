using Asp.Versioning;
using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.IndividualDashboard;
using LifeBalance.Dashboard.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/individual")]
[Authorize(Policy = Policies.DashboardRead)]
[EnableRateLimiting("fixed")]
public class IndividualDashboardController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public IndividualDashboardController(ICurrentUserService currentUserService)
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

    [HttpGet]
    [ProducesResponseType(typeof(IndividualDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIndividualDashboard(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualDashboardQuery(userId), cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualSummaryQuery(userId), cancellationToken));
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualKpisQuery(userId), cancellationToken));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualStatisticsQuery(userId), cancellationToken));
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualHeatmapQuery(userId), cancellationToken));
    }

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualGoalsQuery(userId), cancellationToken));
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualProgressQuery(userId), cancellationToken));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualActivityQuery(userId), cancellationToken));
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualRecommendationsQuery(userId), cancellationToken));
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> GetRewards(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualRewardsQuery(userId), cancellationToken));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualNotificationsQuery(userId), cancellationToken));
    }

    [HttpGet("biometrics")]
    public async Task<IActionResult> GetBiometrics(CancellationToken cancellationToken)
    {
        var authResult = ResolveUserId(out var userId);
        if (authResult is not null)
        {
            return authResult;
        }

        return HandleResult(await Mediator.Send(new GetIndividualBiometricsQuery(userId), cancellationToken));
    }
}
