using MediatR;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.OrganizationSaaS.Application.Features.Organizations;
using LifeBalance.OrganizationSaaS.Domain.ValueObjects;

namespace LifeBalance.OrganizationSaaS.Api.Controllers.v1;

[ApiController]
[Route("api/v1/organizations")]
[Produces("application/json")]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrgRequest request)
    {
        var command = new CreateOrganizationCommand(request.Name, request.TaxId, request.PlanId, request.ContactInfo, request.Address);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetOrganizationsPagedQuery(pageIndex, pageSize, search));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _mediator.Send(new GetOrganizationByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOrgRequest request)
    {
        var command = new UpdateOrganizationCommand(id, request.Name, request.TaxId, request.ContactInfo, request.Address);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _mediator.Send(new SuspendOrganizationCommand(id));
        return Ok(result);
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(string id)
    {
        var result = await _mediator.Send(new ActivateOrganizationCommand(id));
        return Ok(result);
    }

    [HttpPatch("{id}/suspend")]
    public async Task<IActionResult> Suspend(string id)
    {
        var result = await _mediator.Send(new SuspendOrganizationCommand(id));
        return Ok(result);
    }

    [HttpPatch("{id}/restore")]
    public async Task<IActionResult> Restore(string id)
    {
        var result = await _mediator.Send(new RestoreOrganizationCommand(id));
        return Ok(result);
    }

    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetStatistics(string id)
    {
        var result = await _mediator.Send(new GetOrganizationStatsQuery(id));
        return Ok(result);
    }
}

public record CreateOrgRequest(string Name, string TaxId, string PlanId, ContactInfo ContactInfo, Address Address);
public record UpdateOrgRequest(string Name, string TaxId, ContactInfo ContactInfo, Address Address);
