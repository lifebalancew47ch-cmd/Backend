using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/emails")]
public class EmailsController : ControllerBase
{
    private readonly IEmailService _emailService;
    public EmailsController(IEmailService emailService) { _emailService = emailService; }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendEmailDto dto)
    {
        var result = await _emailService.SendAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("template")]
    public async Task<IActionResult> SendTemplate([FromBody] EmailTemplateDto dto)
    {
        var result = await _emailService.SendTemplateAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> SendBulk([FromBody] BulkEmailDto dto)
    {
        var result = await _emailService.SendBulkAsync(dto);
        return Ok(new Response<List<NotificationResponseDto>>(result));
    }
}
