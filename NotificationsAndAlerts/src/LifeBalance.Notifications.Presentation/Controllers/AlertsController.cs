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
[Route("api/v1/alerts")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    public AlertsController(IAlertService alertService) { _alertService = alertService; }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("User identifier claim not found", StatusCodes.Status401Unauthorized);
        return userId;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertDto dto)
    {
        // Auditoria 6/08/2026: CreateAlertDto.UserId llegaba tal cual del
        // cliente sin contrastarse contra el token (BOLA de escritura -
        // cualquier usuario autenticado podia crear una alerta a nombre de
        // otro). Salvo ADMIN, el UserId real siempre es el del llamante.
        if (!User.IsInRole("ADMIN"))
        {
            dto.UserId = GetUserId();
        }
        var result = await _alertService.CreateAsync(dto);
        return Ok(new Response<AlertDto>(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var results = await _alertService.GetAllAsync(userId);
        return Ok(new Response<List<AlertDto>>(results));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var userId = GetUserId();
        var result = await _alertService.GetByIdAsync(id);
        if (result is null) return NotFound(Response<string>.Fail("Alert not found"));
        if (result.UserId != userId) return Forbid();
        return Ok(new Response<AlertDto>(result));
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var userId = GetUserId();
        var alert = await _alertService.GetByIdAsync(id);
        if (alert is null) return NotFound(Response<string>.Fail("Alert not found"));
        if (alert.UserId != userId) return Forbid();
        var result = await _alertService.MarkAsReadAsync(id);
        if (!result) return NotFound(Response<string>.Fail("Alert not found"));
        return Ok(new Response<string>("Alert marked as read"));
    }

    [HttpPatch("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(string id)
    {
        var userId = GetUserId();
        var alert = await _alertService.GetByIdAsync(id);
        if (alert is null) return NotFound(Response<string>.Fail("Alert not found"));
        if (alert.UserId != userId) return Forbid();
        var result = await _alertService.DismissAsync(id);
        if (!result) return NotFound(Response<string>.Fail("Alert not found"));
        return Ok(new Response<string>("Alert dismissed"));
    }
}
