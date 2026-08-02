using LifeBalance.Administration.Application.Features.Audit;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class AuditController : AdminControllerBase
{
    public AuditController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? userId = null,
        [FromQuery] string? service = null,
        [FromQuery] AuditEventType? eventType = null,
        [FromQuery] string? organizationId = null,
        [FromQuery] string? companyId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetAuditLogsPagedQuery(
            pageIndex, pageSize, userId, service, eventType, organizationId, companyId, fromDate, toDate));
        return Ok(result);
    }

    [HttpGet("by-user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetAuditLogsByUserQuery(userId, pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("by-service/{service}")]
    public async Task<IActionResult> GetByService(string service, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetAuditLogsByServiceQuery(service, pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAuditLogByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}
