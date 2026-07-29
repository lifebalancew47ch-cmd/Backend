using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/preferences")]
public class PreferencesController : ControllerBase
{
    private readonly IPreferenceService _preferenceService;

    public PreferencesController(IPreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string userId)
    {
        var result = await _preferenceService.GetAsync(userId);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromQuery] string userId, [FromBody] UpdatePreferenceDto dto)
    {
        var result = await _preferenceService.UpdateAsync(userId, dto);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPatch("push")]
    public async Task<IActionResult> UpdatePush([FromQuery] string userId, [FromQuery] bool enabled)
    {
        var result = await _preferenceService.UpdatePushAsync(userId, enabled);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPatch("email")]
    public async Task<IActionResult> UpdateEmail([FromQuery] string userId, [FromQuery] bool enabled)
    {
        var result = await _preferenceService.UpdateEmailAsync(userId, enabled);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPatch("wear")]
    public async Task<IActionResult> UpdateWear([FromQuery] string userId, [FromQuery] bool enabled)
    {
        var result = await _preferenceService.UpdateWearOSAsync(userId, enabled);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }
}
