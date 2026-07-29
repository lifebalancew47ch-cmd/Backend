using MediatR;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.OrganizationSaaS.Application.Features.Families;

namespace LifeBalance.OrganizationSaaS.Api.Controllers.v1;

[ApiController]
[Route("api/v1/families")]
[Produces("application/json")]
public class FamiliesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FamiliesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFamilyCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetFamiliesPagedQuery(pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _mediator.Send(new GetFamilyByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateFamilyRequest request)
    {
        var result = await _mediator.Send(new UpdateFamilyCommand(id, request.Name));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _mediator.Send(new DeleteFamilyCommand(id));
        return Ok(result);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(string id, [FromBody] AddMemberRequest request)
    {
        var result = await _mediator.Send(new AddFamilyMemberCommand(id, request.UserId));
        return Ok(result);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId)
    {
        var result = await _mediator.Send(new RemoveFamilyMemberCommand(id, userId));
        return Ok(result);
    }

    [HttpPatch("{id}/administrator")]
    public async Task<IActionResult> TransferAdmin(string id, [FromBody] TransferAdminRequest request)
    {
        var result = await _mediator.Send(new TransferFamilyAdminCommand(id, request.NewAdminUserId));
        return Ok(result);
    }
}

public record UpdateFamilyRequest(string Name);
public record AddMemberRequest(string UserId);
public record TransferAdminRequest(string NewAdminUserId);
