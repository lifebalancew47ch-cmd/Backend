using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace LifeBalance.Reporting.Infrastructure.HttpClients.DelegatingHandlers;

/// <summary>
/// Propagates the authenticated user's bearer token to outbound upstream requests.
/// </summary>
public sealed class JwtPropagationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of <see cref="JwtPropagationDelegatingHandler"/>.</summary>
    public JwtPropagationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
