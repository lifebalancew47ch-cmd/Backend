using Asp.Versioning;
using LifeBalance.Dashboard.API.Authorization;
using LifeBalance.Dashboard.Application.Features.FamilyDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/family")]
[Authorize(Policy = Policies.DashboardRead)]
public class FamilyDashboardController : ApiControllerBase
{
    private const string DefaultFamilyId = "fam_001";

    private string GetTargetFamilyId(string? familyId) => string.IsNullOrWhiteSpace(familyId) ? DefaultFamilyId : familyId;

    [HttpGet]
    public async Task<IActionResult> GetFamilyDashboard([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyDashboardQuery(GetTargetFamilyId(familyId)), cancellationToken));

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyStatisticsQuery(GetTargetFamilyId(familyId)), cancellationToken));

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyGoalsQuery(GetTargetFamilyId(familyId)), cancellationToken));

    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyRankingQuery(GetTargetFamilyId(familyId)), cancellationToken));

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyMembersQuery(GetTargetFamilyId(familyId)), cancellationToken));

    [HttpGet("challenges")]
    public async Task<IActionResult> GetChallenges([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyChallengesQuery(GetTargetFamilyId(familyId)), cancellationToken));

    [HttpGet("rewards")]
    public async Task<IActionResult> GetRewards([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyRewardsQuery(GetTargetFamilyId(familyId)), cancellationToken));

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] string? familyId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyHeatmapQuery(GetTargetFamilyId(familyId)), cancellationToken));
}
