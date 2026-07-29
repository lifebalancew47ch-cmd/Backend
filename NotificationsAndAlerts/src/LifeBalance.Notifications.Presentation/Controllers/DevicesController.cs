using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceRegistrationService _deviceService;
    public DevicesController(IDeviceRegistrationService deviceService) { _deviceService = deviceService; }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] DeviceRegistrationDto dto)
    {
        await _deviceService.RegisterAsync(dto);
        return Ok(new Response<string>("Device registered"));
    }

    [HttpDelete("unregister")]
    public async Task<IActionResult> Unregister([FromQuery] string userId, [FromQuery] string deviceToken)
    {
        var result = await _deviceService.UnregisterAsync(userId, deviceToken);
        if (!result) return NotFound(new Response<string>("Device not found"));
        return Ok(new Response<string>("Device unregistered"));
    }
}
