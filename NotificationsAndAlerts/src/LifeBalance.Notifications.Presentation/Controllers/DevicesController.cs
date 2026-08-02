using System.Security.Claims;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Exceptions;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("fixed")]
[Route("api/v1/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceRegistrationService _deviceService;
    public DevicesController(IDeviceRegistrationService deviceService) { _deviceService = deviceService; }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("User identifier claim not found", StatusCodes.Status401Unauthorized);
        return userId;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] DeviceRegistrationDto dto)
    {
        dto.UserId = GetUserId();
        await _deviceService.RegisterAsync(dto);
        return Ok(new Response<string>("Device registered"));
    }

    [HttpDelete("unregister")]
    public async Task<IActionResult> Unregister([FromQuery] string deviceToken)
    {
        var userId = GetUserId();
        var result = await _deviceService.UnregisterAsync(userId, deviceToken);
        if (!result) return NotFound(Response<string>.Fail("Device not found"));
        return Ok(new Response<string>("Device unregistered"));
    }
}
