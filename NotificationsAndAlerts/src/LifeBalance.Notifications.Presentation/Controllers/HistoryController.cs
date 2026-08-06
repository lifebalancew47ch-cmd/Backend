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
    [Authorize(Roles = "ADMIN")]
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
        // Auditoria 6/08/2026 (S-20): este endpoint filtraba por el
        // organizationId de la URL sin verificar que el llamante perteneciera
        // a esa organizacion (BOLA - OWASP API1:2023). Un usuario autenticado
        // de la organizacion A podia leer el historial de notificaciones de
        // la organizacion B con solo cambiar el id en la ruta. Confirmado con
        // pruebas cruzadas: GetByUser() devolvia datos propios mientras que
        // esta ruta devolvia 200 tanto para la organizacion propia como para
        // una ajena (ambas vacias en las pruebas, pero el filtro por path
        // seguia activo). Se exige ahora que el organizationId coincida con
        // el claim del token, salvo para ADMIN (igual que GetAll()).
        if (!User.IsInRole("ADMIN"))
        {
            var callerOrganizationId = User.FindFirst("organization_id")?.Value;
            if (string.IsNullOrEmpty(callerOrganizationId) || callerOrganizationId != organizationId)
                return Forbid();
        }

        var result = await _historyService.GetByOrganizationAsync(organizationId);
        return Ok(new Response<List<NotificationHistoryDto>>(result));
    }
}
