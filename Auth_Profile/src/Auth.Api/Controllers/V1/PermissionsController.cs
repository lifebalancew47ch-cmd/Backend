using Auth.Application.Commands.Permissions;
using Auth.Application.Queries.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auth.Api.Controllers.V1;

public class PermissionsController : BaseController
{
    [HttpGet(Name = "GetPermissions")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<IEnumerable<Auth.Application.DTOs.Permissions.PermissionDto>>), 200)]
    [SwaggerOperation(Summary = "Get all permissions", Description = "Returns all permissions. Requires Admin role.")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAllPermissionsQuery(), ct);
        return HandleResponse(result);
    }

    [HttpPost(Name = "CreatePermission")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Permissions.PermissionDto>), 201)]
    [SwaggerOperation(Summary = "Create a permission", Description = "Creates a new permission. Requires Admin role.")]
    public async Task<IActionResult> Create([FromBody] Auth.Application.DTOs.Permissions.CreatePermissionRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreatePermissionCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPut("{id}", Name = "UpdatePermission")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Permissions.PermissionDto>), 200)]
    [SwaggerOperation(Summary = "Update a permission", Description = "Updates an existing permission. Requires Admin role.")]
    public async Task<IActionResult> Update(string id, [FromBody] Auth.Application.DTOs.Permissions.UpdatePermissionRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdatePermissionCommand(id, request), ct);
        return HandleResponse(result);
    }

    [HttpDelete("{id}", Name = "DeletePermission")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [SwaggerOperation(Summary = "Delete a permission", Description = "Deletes a permission. Requires Admin role.")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeletePermissionCommand(id), ct);
        return HandleResponse(result);
    }
}
