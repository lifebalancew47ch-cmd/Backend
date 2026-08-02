using LifeBalance.Administration.Application.Features.Parameters;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class ParametersController : AdminControllerBase
{
    public ParametersController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParameterRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateParameterCommand(
            request.Code, request.Name, request.Description, request.DataType, request.Value,
            request.Category, request.MinValue, request.MaxValue, request.Unit, request.Order), cancellationToken);

        await RecordAuditAsync(
            "PARAMETER_CREATE", "SystemParameter", result.Data?.Id ?? string.Empty,
            AuditOperationType.Create, AuditEventType.Parameter, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? onlyActive = null)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetParametersPagedQuery(pageIndex, pageSize, search, category, onlyActive));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetParameterByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateParameterRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateParameterCommand(
            id, request.Name, request.Description, request.DataType, request.Value,
            request.Category, request.MinValue, request.MaxValue, request.Unit, request.Order), cancellationToken);

        await RecordAuditAsync(
            "PARAMETER_UPDATE", "SystemParameter", id,
            AuditOperationType.Update, AuditEventType.Parameter, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteParameterCommand(id), cancellationToken);

        await RecordAuditAsync(
            "PARAMETER_DELETE", "SystemParameter", id,
            AuditOperationType.Delete, AuditEventType.Parameter, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetParameterStatusCommand(id, true), cancellationToken);

        await RecordAuditAsync(
            "PARAMETER_ACTIVATE", "SystemParameter", id,
            AuditOperationType.Patch, AuditEventType.Parameter, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetParameterStatusCommand(id, false), cancellationToken);

        await RecordAuditAsync(
            "PARAMETER_DEACTIVATE", "SystemParameter", id,
            AuditOperationType.Patch, AuditEventType.Parameter, cancellationToken: cancellationToken);

        return Ok(result);
    }
}

public record CreateParameterRequest(
    string Code,
    string Name,
    string Description,
    ParameterDataType DataType,
    string Value,
    string Category,
    string? MinValue = null,
    string? MaxValue = null,
    string Unit = "",
    int Order = 0);

public record UpdateParameterRequest(
    string Name,
    string Description,
    ParameterDataType DataType,
    string Value,
    string Category,
    string? MinValue = null,
    string? MaxValue = null,
    string Unit = "",
    int Order = 0);
