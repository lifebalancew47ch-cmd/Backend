using LifeBalance.Administration.Application.Features.Integrations;
using LifeBalance.Administration.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

/// <summary>
/// Live read endpoints that proxy data from the microservices this administration
/// API orchestrates: Auth &amp; Profile (roles and permissions) and Organization &amp;
/// SaaS (organizational configuration). Responses are fail-closed (503 when the
/// upstream service is unavailable).
/// </summary>
public class IntegrationsController : AdminControllerBase
{
    public IntegrationsController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpGet("auth/roles")]
    public async Task<IActionResult> GetAuthRoles(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAuthRolesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("auth/permissions")]
    public async Task<IActionResult> GetAuthPermissions(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAuthPermissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetOrganizationConfiguration(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetOrganizationConfigurationQuery(), cancellationToken);
        return Ok(result);
    }
}
