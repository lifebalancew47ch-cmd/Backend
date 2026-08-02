using System.ComponentModel.DataAnnotations;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("fixed")]
[Route("api/v1/emails")]
public class EmailsController : ControllerBase
{
    private static readonly EmailAddressAttribute _emailValidator = new();

    private const int MaxBulkRecipients = 500;

    private readonly IEmailService _emailService;
    public EmailsController(IEmailService emailService) { _emailService = emailService; }

    private IActionResult? ValidateRecipients(List<string>? recipients)
    {
        var list = recipients ?? new List<string>();
        if (list.Count == 0)
            return BadRequest(Response<string>.Fail("At least one recipient is required"));
        if (list.Count > MaxBulkRecipients)
            return BadRequest(Response<string>.Fail($"Bulk email limit is {MaxBulkRecipients} recipients"));
        foreach (var recipient in list)
        {
            if (!_emailValidator.IsValid(recipient))
                return BadRequest(Response<string>.Fail("One or more recipients have an invalid email format"));
        }
        return null;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendEmailDto dto)
    {
        var result = await _emailService.SendAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("template")]
    public async Task<IActionResult> SendTemplate([FromBody] EmailTemplateDto dto)
    {
        var validationError = ValidateRecipients(dto.To);
        if (validationError is not null) return validationError;
        var result = await _emailService.SendTemplateAsync(dto);
        return Ok(new Response<NotificationResponseDto>(result));
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> SendBulk([FromBody] BulkEmailDto dto)
    {
        var validationError = ValidateRecipients(dto.To);
        if (validationError is not null) return validationError;
        var result = await _emailService.SendBulkAsync(dto);
        return Ok(new Response<List<NotificationResponseDto>>(result));
    }
}
