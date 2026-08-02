using LifeBalance.Administration.Application.Features.Settings;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class SettingsController : AdminControllerBase
{
    public SettingsController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UpdateSettingsCommand(request, CurrentUser.UserId), cancellationToken);

        await RecordAuditAsync(
            "SETTINGS_UPDATE", "SystemConfiguration", result.Data?.Id ?? "system",
            AuditOperationType.Update, AuditEventType.Configuration, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ResetSettingsCommand(CurrentUser.UserId), cancellationToken);

        await RecordAuditAsync(
            "SETTINGS_RESET", "SystemConfiguration", result.Data?.Id ?? "system",
            AuditOperationType.Update, AuditEventType.Configuration, cancellationToken: cancellationToken);

        return Ok(result);
    }
}
