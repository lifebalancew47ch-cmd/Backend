using Asp.Versioning;
using Auth.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IMediator Mediator => HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected IActionResult HandleResponse<T>(ApiResponse<T> response)
    {
        return StatusCode(response.StatusCode, response);
    }
}
