using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/history")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;
    public HistoryController(IHistoryService historyService) { _historyService = historyService; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _historyService.GetAllAsync(page, pageSize);
        return Ok(new Response<PaginatedResult<NotificationHistoryDto>>(result));
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var result = await _historyService.GetByUserAsync(userId);
        return Ok(new Response<List<NotificationHistoryDto>>(result));
    }

    [HttpGet("organization/{organizationId}")]
    public async Task<IActionResult> GetByOrganization(string organizationId)
    {
        var result = await _historyService.GetByOrganizationAsync(organizationId);
        return Ok(new Response<List<NotificationHistoryDto>>(result));
    }
}
