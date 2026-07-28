using Auth.Application.Commands.Roles;
using Auth.Application.Queries.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auth.Api.Controllers.V1;

public class RolesController : BaseController
{
    [HttpGet(Name = "GetRoles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<IEnumerable<Auth.Application.DTOs.Roles.RoleDto>>), 200)]
    [SwaggerOperation(Summary = "Get all roles", Description = "Returns all roles. Requires Admin role.")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAllRolesQuery(), ct);
        return HandleResponse(result);
    }

    [HttpPost(Name = "CreateRole")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Roles.RoleDto>), 201)]
    [SwaggerOperation(Summary = "Create a role", Description = "Creates a new role. Requires Admin role.")]
    public async Task<IActionResult> Create([FromBody] Auth.Application.DTOs.Roles.CreateRoleRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateRoleCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPut("{id}", Name = "UpdateRole")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Roles.RoleDto>), 200)]
    [SwaggerOperation(Summary = "Update a role", Description = "Updates an existing role. Requires Admin role.")]
    public async Task<IActionResult> Update(string id, [FromBody] Auth.Application.DTOs.Roles.UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateRoleCommand(id, request), ct);
        return HandleResponse(result);
    }

    [HttpDelete("{id}", Name = "DeleteRole")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [SwaggerOperation(Summary = "Delete a role", Description = "Deletes a role. Requires Admin role.")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeleteRoleCommand(id), ct);
        return HandleResponse(result);
    }
}
