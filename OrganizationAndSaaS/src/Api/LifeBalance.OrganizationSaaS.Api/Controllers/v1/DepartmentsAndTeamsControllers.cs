using MediatR;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.OrganizationSaaS.Application.Features.DepartmentsAndTeams;

namespace LifeBalance.OrganizationSaaS.Api.Controllers.v1;

[ApiController]
[Route("api/v1/departments")]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string organizationId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetDepartmentsPagedQuery(organizationId, pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _mediator.Send(new GetDepartmentByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDeptRequest request)
    {
        var result = await _mediator.Send(new UpdateDepartmentCommand(id, request.Name, request.Description, request.ManagerUserId, request.ParentDepartmentId));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _mediator.Send(new DeleteDepartmentCommand(id));
        return Ok(result);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AssignMember(string id, [FromBody] DeptMemberRequest request)
    {
        var result = await _mediator.Send(new AssignDepartmentMemberCommand(id, request.UserId));
        return Ok(result);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId)
    {
        var result = await _mediator.Send(new RemoveDepartmentMemberCommand(id, userId));
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/teams")]
[Produces("application/json")]
public class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string organizationId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetTeamsPagedQuery(organizationId, pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _mediator.Send(new GetTeamByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamRequest request)
    {
        var result = await _mediator.Send(new UpdateTeamCommand(id, request.Name, request.DepartmentId, request.LeaderUserId));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _mediator.Send(new DeleteTeamCommand(id));
        return Ok(result);
    }
}

public record UpdateDeptRequest(string Name, string Description, string? ManagerUserId = null, string? ParentDepartmentId = null);
public record DeptMemberRequest(string UserId);
public record UpdateTeamRequest(string Name, string? DepartmentId = null, string? LeaderUserId = null);
