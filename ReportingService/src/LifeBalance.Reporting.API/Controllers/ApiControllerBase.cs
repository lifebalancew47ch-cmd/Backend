using MediatR;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.API.Controllers;

/// <summary>
/// Base controller providing access to the MediatR sender and standardized responses.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    /// <summary>Gets the MediatR sender.</summary>
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Converts a <see cref="Result{T}"/> into a standardized <see cref="ApiResponse{T}"/>.
    /// Success produces 200 OK; failure produces 400 Bad Request.
    /// </summary>
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
