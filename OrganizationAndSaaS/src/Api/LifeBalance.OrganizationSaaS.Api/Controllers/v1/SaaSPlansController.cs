using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.OrganizationSaaS.Application.Features.SaaSPlans;

namespace LifeBalance.OrganizationSaaS.Api.Controllers.v1;

[ApiController]
[Route("api/v1/plans")]
[Produces("application/json")]
public class SaaSPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public SaaSPlansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveSaaSPlansQuery(100), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaaSPlanByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateSaaSPlanCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSaaSPlanRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSaaSPlanCommand(id, request.Name, request.Tier, request.PriceMonthly,
            request.PriceYearly, request.Currency, request.IsCustomPricing, request.IsHighlighted,
            request.Features, request.Limits);
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new SetSaaSPlanActiveCommand(id, true), cancellationToken));

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new SetSaaSPlanActiveCommand(id, false), cancellationToken));
}

public record UpdateSaaSPlanRequest(
    [property: System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2)] string Name,
    [property: System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(30)] string Tier,
    [property: System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0", "999999999")] decimal PriceMonthly,
    [property: System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0", "999999999")] decimal PriceYearly,
    [property: System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.RegularExpression("^[A-Za-z]{3}$")] string Currency,
    bool IsCustomPricing,
    bool IsHighlighted,
    [property: System.ComponentModel.DataAnnotations.MaxLength(50)] List<string> Features,
    [property: System.ComponentModel.DataAnnotations.Required] PlanLimitsDto Limits);
