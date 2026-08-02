using LifeBalance.Administration.Application.Features.FeatureFlags;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class FeatureFlagsController : AdminControllerBase
{
    public FeatureFlagsController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateFeatureFlagCommand(
            request.Code, request.Name, request.Description, request.Category, request.IsSystem), cancellationToken);

        await RecordAuditAsync(
            "FEATURE_FLAG_CREATE", "FeatureFlag", result.Data?.Id ?? string.Empty,
            AuditOperationType.Create, AuditEventType.Module, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? onlyEnabled = null)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetFeatureFlagsPagedQuery(pageIndex, pageSize, search, category, onlyEnabled));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetFeatureFlagByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateFeatureFlagCommand(
            id, request.Name, request.Description, request.Category), cancellationToken);

        await RecordAuditAsync(
            "FEATURE_FLAG_UPDATE", "FeatureFlag", id,
            AuditOperationType.Update, AuditEventType.Module, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteFeatureFlagCommand(id), cancellationToken);

        await RecordAuditAsync(
            "FEATURE_FLAG_DELETE", "FeatureFlag", id,
            AuditOperationType.Delete, AuditEventType.Module, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetFeatureFlagStatusCommand(id, true), cancellationToken);

        await RecordAuditAsync(
            "FEATURE_FLAG_ENABLE", "FeatureFlag", id,
            AuditOperationType.Patch, AuditEventType.Module, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetFeatureFlagStatusCommand(id, false), cancellationToken);

        await RecordAuditAsync(
            "FEATURE_FLAG_DISABLE", "FeatureFlag", id,
            AuditOperationType.Patch, AuditEventType.Module, cancellationToken: cancellationToken);

        return Ok(result);
    }
}

public record CreateFeatureFlagRequest(
    string Code,
    string Name,
    string Description,
    string Category,
    bool IsSystem = false);

public record UpdateFeatureFlagRequest(
    string Name,
    string Description,
    string Category);
