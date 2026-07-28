using MediatR;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.Dashboard.Shared.Results;

namespace LifeBalance.Dashboard.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            var traceId = HttpContext.TraceIdentifier;
            return Ok(ApiResponse<T>.Ok(result.Value, "Request processed successfully.", traceId));
        }

        return BadRequest(ApiResponse<T>.Fail(result.Error ?? "An error occurred.", statusCode: 400, traceId: HttpContext.TraceIdentifier));
    }
}
