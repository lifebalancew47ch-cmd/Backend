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
[Route("api/v1/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    public MetricsController(IMetricsService metricsService) { _metricsService = metricsService; }

    [HttpGet]
    public async Task<IActionResult> GetGeneral()
    {
        var result = await _metricsService.GetGeneralAsync();
        return Ok(new Response<MetricsDto>(result));
    }

    [HttpGet("delivery")]
    public async Task<IActionResult> GetDelivery()
    {
        var result = await _metricsService.GetDeliveryAsync();
        return Ok(new Response<DeliveryMetricsDto>(result));
    }

    [HttpGet("channels")]
    public async Task<IActionResult> GetChannels()
    {
        var result = await _metricsService.GetChannelsAsync();
        return Ok(new Response<List<ChannelMetricsDto>>(result));
    }

    [HttpGet("errors")]
    public async Task<IActionResult> GetErrors()
    {
        var result = await _metricsService.GetErrorsAsync();
        return Ok(new Response<ErrorMetricsDto>(result));
    }
}
