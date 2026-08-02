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
[Route("api/v1/history")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;
    public HistoryController(IHistoryService historyService) { _historyService = historyService; }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("User identifier claim not found", StatusCodes.Status401Unauthorized);
        return userId;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _historyService.GetAllAsync(page, pageSize);
        return Ok(new Response<PaginatedResult<NotificationHistoryDto>>(result));
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetByUser()
    {
        var userId = GetUserId();
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
