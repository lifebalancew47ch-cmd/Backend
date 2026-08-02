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
[Route("api/v1/preferences")]
public class PreferencesController : ControllerBase
{
    private readonly IPreferenceService _preferenceService;
    public PreferencesController(IPreferenceService preferenceService) { _preferenceService = preferenceService; }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("User identifier claim not found", StatusCodes.Status401Unauthorized);
        return userId;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetUserId();
        var result = await _preferenceService.GetAsync(userId);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePreferenceDto dto)
    {
        var userId = GetUserId();
        var result = await _preferenceService.UpdateAsync(userId, dto);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPatch("push")]
    public async Task<IActionResult> UpdatePush([FromQuery] bool enabled)
    {
        var userId = GetUserId();
        var result = await _preferenceService.UpdatePushAsync(userId, enabled);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPatch("email")]
    public async Task<IActionResult> UpdateEmail([FromQuery] bool enabled)
    {
        var userId = GetUserId();
        var result = await _preferenceService.UpdateEmailAsync(userId, enabled);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPatch("wear")]
    public async Task<IActionResult> UpdateWear([FromQuery] bool enabled)
    {
        var userId = GetUserId();
        var result = await _preferenceService.UpdateWearOSAsync(userId, enabled);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }
}
