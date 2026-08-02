using LifeBalance.Administration.Application.Features.Logs;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Administration.Api.Controllers.v1;

public class LogsController : AdminControllerBase
{
    public LogsController(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
        : base(mediator, currentUser, audit) { }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] LogEntryRequest entry, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new IngestLogCommand(entry), cancellationToken);
        return Ok(result);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> IngestBulk([FromBody] IReadOnlyList<LogEntryRequest> entries, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new IngestLogsCommand(entries), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] MicroserviceName? service = null,
        [FromQuery] SystemLogLevel? level = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetSystemLogsPagedQuery(
            pageIndex, pageSize, service, level, userId, correlationId, fromDate, toDate));
        return Ok(result);
    }

    [HttpGet("errors")]
    public async Task<IActionResult> GetErrors([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetErrorLogsQuery(pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("warnings")]
    public async Task<IActionResult> GetWarnings([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await Mediator.Send(new GetWarningLogsQuery(pageIndex, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSystemLogByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}
