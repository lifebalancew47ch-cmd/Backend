using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.OrganizationSaaS.Application.Features.LicensesAndSubscriptions;

namespace LifeBalance.OrganizationSaaS.Api.Controllers.v1;

[ApiController]
[Route("api/v1/licenses")]
[Produces("application/json")]
public class LicensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LicensesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLicenseCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string organizationId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetLicensesPagedQuery(organizationId, pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _mediator.Send(new GetLicenseByIdQuery(id));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Revoke(string id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand(id));
        return Ok(result);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignLicenseRequest request)
    {
        var result = await _mediator.Send(new AssignLicenseCommand(id, request.UserId));
        return Ok(result);
    }

    [HttpPost("{id}/renew")]
    public async Task<IActionResult> Renew(string id, [FromBody] RenewLicenseRequest request)
    {
        var result = await _mediator.Send(new RenewLicenseCommand(id, request.NewExpiration));
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/subscriptions")]
[Produces("application/json")]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetSubscriptionsPagedQuery(pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _mediator.Send(new GetSubscriptionByIdQuery(id));
        return Ok(result);
    }

    [HttpPatch("{id}/renew")]
    public async Task<IActionResult> Renew(string id)
    {
        var result = await _mediator.Send(new RenewSubscriptionCommand(id));
        return Ok(result);
    }

    [HttpPatch("{id}/change-plan")]
    public async Task<IActionResult> ChangePlan(string id, [FromBody] ChangePlanRequest request)
    {
        var result = await _mediator.Send(new ChangeSubscriptionPlanCommand(id, request.NewPlanId));
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/invitations")]
[Produces("application/json")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvitationsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvitationCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetInvitationsPagedQuery(pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _mediator.Send(new GetInvitationByIdQuery(id));
        return Ok(result);
    }

    [HttpPost("{token}/accept")]
    [AllowAnonymous] // Public by design: invitation token in the URL is the credential
    public async Task<IActionResult> Accept(string token)
    {
        var result = await _mediator.Send(new AcceptInvitationCommand(token));
        return Ok(result);
    }

    [HttpPost("{token}/reject")]
    [AllowAnonymous] // Public by design: invitation token in the URL is the credential
    public async Task<IActionResult> Reject(string token)
    {
        var result = await _mediator.Send(new RejectInvitationCommand(token));
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        var result = await _mediator.Send(new CancelInvitationCommand(id));
        return Ok(result);
    }

    [HttpPost("{id}/resend")]
    public async Task<IActionResult> Resend(string id)
    {
        var result = await _mediator.Send(new ResendInvitationCommand(id));
        return Ok(result);
    }
}

public record AssignLicenseRequest(string UserId);
public record RenewLicenseRequest(DateTime NewExpiration);
public record ChangePlanRequest(string NewPlanId);
