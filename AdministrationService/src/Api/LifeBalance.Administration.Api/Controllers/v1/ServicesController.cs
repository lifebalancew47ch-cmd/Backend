using LifeBalance.Administration.Application.Features.Services;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class ServicesController : AdminControllerBase
{
    public ServicesController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpGet("status")]
    public async Task<IActionResult> GetBoard([FromQuery] bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetServicesStatusQuery(forceRefresh), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{service}/status")]
    public async Task<IActionResult> GetService(MicroserviceName service, [FromQuery] bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetServiceStatusQuery(service, forceRefresh), cancellationToken);
        return Ok(result);
    }
}
