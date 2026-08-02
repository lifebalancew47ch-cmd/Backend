using LifeBalance.Administration.Application.Features.Maintenance;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class MaintenanceController : AdminControllerBase
{
    public MaintenanceController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMaintenanceStatusQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("status")]
    public async Task<IActionResult> SetStatus([FromBody] SetMaintenanceModeRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new SetMaintenanceModeCommand(request.IsEnabled, request.Message, request.ScheduledEndAt, CurrentUser.UserId),
            cancellationToken);

        await RecordAuditAsync(
            request.IsEnabled ? "MAINTENANCE_ENABLE" : "MAINTENANCE_DISABLE",
            "MaintenanceMode", "system",
            AuditOperationType.Patch, AuditEventType.Maintenance, cancellationToken: cancellationToken);

        return Ok(result);
    }
}

public record SetMaintenanceModeRequest(bool IsEnabled, string Message = "", DateTime? ScheduledEndAt = null);
