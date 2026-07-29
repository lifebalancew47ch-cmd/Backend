using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IHistoryService _historyService;
    private readonly IPreferenceService _preferenceService;
    private readonly IScheduleService _scheduleService;
    private readonly ITemplateService _templateService;

    public NotificationsController(
        INotificationService notificationService,
        IHistoryService historyService,
        IPreferenceService preferenceService,
        IScheduleService scheduleService,
        ITemplateService templateService)
    {
        _notificationService = notificationService;
        _historyService = historyService;
        _preferenceService = preferenceService;
        _scheduleService = scheduleService;
        _templateService = templateService;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendNotificationDto dto)
    {
        var result = await _notificationService.SendAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> SendBulk([FromBody] List<SendNotificationDto> dtos)
    {
        var results = await _notificationService.SendBulkAsync(dtos);
        return Ok(new Response<List<NotificationResponseDto>>(results));
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> Schedule([FromBody] ScheduleNotificationDto dto)
    {
        var result = await _notificationService.ScheduleAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? userId, [FromQuery] string? organizationId, [FromQuery] string? familyId, [FromQuery] string? departmentId)
    {
        var results = await _notificationService.GetAllAsync(userId, organizationId, familyId, departmentId);
        return Ok(new Response<List<NotificationResponseDto>>(results));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _notificationService.GetByIdAsync(id);
        if (result is null)
            return NotFound(new Response<string>("Notification not found"));
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _notificationService.DeleteAsync(id);
        if (!result) return NotFound(new Response<string>("Notification not found"));
        return Ok(new Response<string>("Notification deleted"));
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        var result = await _notificationService.CancelAsync(id);
        if (!result) return NotFound(new Response<string>("Notification not found or already sent"));
        return Ok(new Response<string>("Notification cancelled"));
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        if (!result) return NotFound(new Response<string>("Notification not found"));
        return Ok(new Response<string>("Notification marked as read"));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead([FromQuery] string userId)
    {
        var result = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new Response<string>($"{result} notifications marked as read"));
    }

    [HttpPatch("{id}/archive")]
    public async Task<IActionResult> Archive(string id)
    {
        var result = await _notificationService.ArchiveAsync(id);
        if (!result) return NotFound(new Response<string>("Notification not found"));
        return Ok(new Response<string>("Notification archived"));
    }

    [HttpPatch("{id}/favorite")]
    public async Task<IActionResult> Favorite(string id)
    {
        var result = await _notificationService.FavoriteAsync(id);
        if (!result) return NotFound(new Response<string>("Notification not found"));
        return Ok(new Response<string>("Notification favorite toggled"));
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(string userId, [FromQuery] int limit = 10)
    {
        var history = await _historyService.GetByUserAsync(userId);
        var items = history.Take(limit).Select(n => new NotificationItemDto(
            n.Id,
            n.Title,
            n.Body,
            n.Type.ToString(),
            n.CreatedAt,
            n.IsRead
        )).ToList();

        return Ok(items);
    }
}
