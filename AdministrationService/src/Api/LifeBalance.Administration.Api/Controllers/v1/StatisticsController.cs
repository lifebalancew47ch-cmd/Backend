using LifeBalance.Administration.Application.Features.Statistics;
using LifeBalance.Administration.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class StatisticsController : AdminControllerBase
{
    public StatisticsController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAdministrativeStatisticsQuery(), cancellationToken);
        return Ok(result);
    }
}
