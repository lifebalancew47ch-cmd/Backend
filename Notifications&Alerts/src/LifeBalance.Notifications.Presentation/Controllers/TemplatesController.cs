using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace LifeBalance.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/v1/templates")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public TemplatesController(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTemplateDto dto)
    {
        var result = await _templateService.CreateAsync(dto);
        return Ok(new Response<TemplateDto>(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var results = await _templateService.GetAllAsync();
        return Ok(new Response<List<TemplateDto>>(results));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _templateService.GetByIdAsync(id);
        if (result is null)
            return NotFound(new Response<string>("Template not found"));

        return Ok(new Response<TemplateDto>(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateTemplateDto dto)
    {
        var result = await _templateService.UpdateAsync(id, dto);
        if (result is null)
            return NotFound(new Response<string>("Template not found"));

        return Ok(new Response<TemplateDto>(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _templateService.DeleteAsync(id);
        if (!result)
            return NotFound(new Response<string>("Template not found"));

        return Ok(new Response<string>("Template deleted"));
    }
}
