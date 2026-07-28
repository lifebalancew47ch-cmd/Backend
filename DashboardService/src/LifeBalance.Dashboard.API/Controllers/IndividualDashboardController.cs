using Asp.Versioning;
using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Application.Features.IndividualDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/individual")]
[Authorize(Policy = Policies.DashboardRead)]
public class IndividualDashboardController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public IndividualDashboardController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    private string GetTargetUserId(string? userId) => string.IsNullOrWhiteSpace(userId) ? (_currentUserService.UserId ?? "usr_default") : userId;

    [HttpGet]
    [ProducesResponseType(typeof(IndividualDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIndividualDashboard([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualDashboardQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualSummaryQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualKpisQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualStatisticsQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualHeatmapQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualGoalsQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualProgressQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualActivityQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualRecommendationsQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("rewards")]
    public async Task<IActionResult> GetRewards([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualRewardsQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualNotificationsQuery(GetTargetUserId(userId)), cancellationToken));

    [HttpGet("biometrics")]
    public async Task<IActionResult> GetBiometrics([FromQuery] string? userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIndividualBiometricsQuery(GetTargetUserId(userId)), cancellationToken));
}
