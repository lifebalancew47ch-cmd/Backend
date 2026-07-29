using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/alerts")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertDto dto)
    {
        var result = await _alertService.CreateAsync(dto);
        return Ok(new Response<AlertDto>(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string userId)
    {
        var results = await _alertService.GetAllAsync(userId);
        return Ok(new Response<List<AlertDto>>(results));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _alertService.GetByIdAsync(id);
        if (result is null)
            return NotFound(new Response<string>("Alert not found"));

        return Ok(new Response<AlertDto>(result));
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var result = await _alertService.MarkAsReadAsync(id);
        if (!result)
            return NotFound(new Response<string>("Alert not found"));

        return Ok(new Response<string>("Alert marked as read"));
    }

    [HttpPatch("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(string id)
    {
        var result = await _alertService.DismissAsync(id);
        if (!result)
            return NotFound(new Response<string>("Alert not found"));

        return Ok(new Response<string>("Alert dismissed"));
    }
}
