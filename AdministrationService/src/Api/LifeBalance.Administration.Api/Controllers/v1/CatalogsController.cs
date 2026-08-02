using LifeBalance.Administration.Application.Features.Catalogs;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class CatalogsController : AdminControllerBase
{
    public CatalogsController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCatalogRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateCatalogCommand(
            request.Code, request.Name, request.Description, request.Category, request.Items), cancellationToken);

        await RecordAuditAsync(
            "CATALOG_CREATE", "Catalog", result.Data?.Id ?? string.Empty,
            AuditOperationType.Create, AuditEventType.Catalog, cancellationToken: cancellationToken);

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
        var result = await Mediator.Send(new GetCatalogsPagedQuery(pageIndex, pageSize, search, category, onlyActive));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCatalogByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCatalogRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateCatalogCommand(
            id, request.Name, request.Description, request.Category, request.Items), cancellationToken);

        await RecordAuditAsync(
            "CATALOG_UPDATE", "Catalog", id,
            AuditOperationType.Update, AuditEventType.Catalog, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteCatalogCommand(id), cancellationToken);

        await RecordAuditAsync(
            "CATALOG_DELETE", "Catalog", id,
            AuditOperationType.Delete, AuditEventType.Catalog, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetCatalogStatusCommand(id, true), cancellationToken);

        await RecordAuditAsync(
            "CATALOG_ACTIVATE", "Catalog", id,
            AuditOperationType.Patch, AuditEventType.Catalog, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetCatalogStatusCommand(id, false), cancellationToken);

        await RecordAuditAsync(
            "CATALOG_DEACTIVATE", "Catalog", id,
            AuditOperationType.Patch, AuditEventType.Catalog, cancellationToken: cancellationToken);

        return Ok(result);
    }
}

public record CreateCatalogRequest(
    string Code,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<CatalogItemRequest>? Items = null);

public record UpdateCatalogRequest(
    string Name,
    string Description,
    string Category,
    IReadOnlyList<CatalogItemRequest>? Items = null);
