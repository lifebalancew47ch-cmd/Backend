using Auth.Application.Queries.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auth.Api.Controllers.V1;

public class AuditController : BaseController
{
    [HttpGet("login-history", Name = "GetLoginHistory")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Shared.Common.PagedResult<Auth.Application.DTOs.Audit.LoginHistoryDto>>), 200)]
    [SwaggerOperation(Summary = "Get login history", Description = "Returns paginated login history. Requires Admin role.")]
    public async Task<IActionResult> GetLoginHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetLoginHistoryQuery(null, page, pageSize), ct);
        return HandleResponse(result);
    }

    [HttpGet("security-events", Name = "GetSecurityEvents")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Shared.Common.PagedResult<Auth.Application.DTOs.Audit.AuditLogDto>>), 200)]
    [SwaggerOperation(Summary = "Get security events", Description = "Returns paginated security audit events. Requires Admin role.")]
    public async Task<IActionResult> GetSecurityEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetSecurityEventsQuery(page, pageSize), ct);
        return HandleResponse(result);
    }
}
