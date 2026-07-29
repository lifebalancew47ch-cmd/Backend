using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("notifications")]
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

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationDto dto)
    {
        var result = await _notificationService.SendAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationDto dto)
    {
        var result = await _notificationService.BroadcastAsync(dto);
        return Ok(new Response<List<NotificationResponseDto>>(result));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string userId)
    {
        var result = await _historyService.GetAllAsync(userId);
        return Ok(new Response<List<NotificationHistoryDto>>(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _historyService.GetByIdAsync(id);
        if (result is null)
            return NotFound(new Response<string>("Notification not found"));

        var dto = new NotificationResponseDto
        {
            Id = result.Id,
            UserId = result.UserId,
            Title = result.Title,
            Body = result.Body,
            Payload = result.Payload,
            Type = result.Type,
            Channel = result.Channel,
            Status = result.Status,
            CreatedAt = result.CreatedAt,
            SentAt = result.SentAt
        };

        return Ok(new Response<NotificationResponseDto>(dto));
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences([FromQuery] string userId)
    {
        var result = await _preferenceService.GetAsync(userId);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromQuery] string userId, [FromBody] UpdatePreferenceDto dto)
    {
        var result = await _preferenceService.UpdateAsync(userId, dto);
        return Ok(new Response<NotificationPreferenceDto>(result));
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> Schedule([FromBody] ScheduleNotificationDto dto)
    {
        var result = await _scheduleService.ScheduleAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpDelete("schedule/{id}")]
    public async Task<IActionResult> CancelSchedule(string id)
    {
        var result = await _scheduleService.CancelAsync(id);
        if (!result)
            return NotFound(new Response<string>("Scheduled notification not found or already cancelled"));

        return Ok(new Response<string>("Scheduled notification cancelled"));
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var result = await _templateService.GetAllAsync();
        return Ok(new Response<List<TemplateDto>>(result));
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateDto dto)
    {
        var result = await _templateService.CreateAsync(dto);
        return Ok(new Response<TemplateDto>(result));
    }

    [HttpPut("templates/{id}")]
    public async Task<IActionResult> UpdateTemplate(string id, [FromBody] CreateTemplateDto dto)
    {
        var result = await _templateService.UpdateAsync(id, dto);
        if (result is null)
            return NotFound(new Response<string>("Template not found"));

        return Ok(new Response<TemplateDto>(result));
    }

    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(string id)
    {
        var result = await _templateService.DeleteAsync(id);
        if (!result)
            return NotFound(new Response<string>("Template not found"));

        return Ok(new Response<string>("Template deleted"));
    }
}
