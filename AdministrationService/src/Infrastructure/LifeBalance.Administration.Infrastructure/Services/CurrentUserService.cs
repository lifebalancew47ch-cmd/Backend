using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using LifeBalance.Administration.Application.Common.Constants;
using LifeBalance.Administration.Application.Interfaces;

namespace LifeBalance.Administration.Infrastructure.Services;

/// <summary>
/// Reads the current administrative user context from the JWT claims and the
/// HTTP request. Anti-IDOR: the userId used for audit is ALWAYS the JWT
/// <see cref="ClaimTypes.NameIdentifier"/> claim, never client-provided values.
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? Context => _httpContextAccessor.HttpContext;

    private ClaimsPrincipal? User => Context?.User;

    public string? UserId =>
        User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User?.FindFirst("sub")?.Value;

    public string? UserEmail =>
        User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public string? UserName =>
        User?.FindFirst(ClaimTypes.Name)?.Value
        ?? User?.FindFirst("name")?.Value;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList()
        ?? new List<string>();

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => Context?.Request.Headers["User-Agent"].ToString();

    public string? CorrelationId => Context?.Request.Headers["X-Correlation-Id"].ToString();

    public string? RequestId => Context?.Request.Headers["X-Request-Id"].ToString()
                                ?? Context?.TraceIdentifier;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdministrator =>
        IsAuthenticated && Roles.Any(r => AdministrationRoles.AllowedAdministrators.Contains(r));
}
