using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/push")]
public class PushController : ControllerBase
{
    private readonly IPushService _pushService;
    public PushController(IPushService pushService) { _pushService = pushService; }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendPushDto dto)
    {
        var result = await _pushService.SendAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastPushDto dto)
    {
        var result = await _pushService.BroadcastAsync(dto);
        return Ok(new Response<List<NotificationResponseDto>>(result));
    }

    [HttpPost("wear")]
    public async Task<IActionResult> SendWear([FromBody] SendPushDto dto)
    {
        dto.Platform = Domain.Enums.DevicePlatform.WearOS;
        var result = await _pushService.SendAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("company")]
    public async Task<IActionResult> SendToCompany([FromBody] BroadcastPushDto dto) { return await Broadcast(dto); }

    [HttpPost("family")]
    public async Task<IActionResult> SendToFamily([FromBody] BroadcastPushDto dto) { return await Broadcast(dto); }

    [HttpPost("department")]
    public async Task<IActionResult> SendToDepartment([FromBody] BroadcastPushDto dto) { return await Broadcast(dto); }
}
