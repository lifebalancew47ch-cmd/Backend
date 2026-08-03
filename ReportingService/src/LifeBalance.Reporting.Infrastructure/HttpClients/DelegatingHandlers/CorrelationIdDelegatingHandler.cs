using Microsoft.AspNetCore.Http;

namespace LifeBalance.Reporting.Infrastructure.HttpClients.DelegatingHandlers;

/// <summary>
/// Propagates the inbound <c>X-Correlation-ID</c> header to outbound upstream requests.
/// </summary>
public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of <see cref="CorrelationIdDelegatingHandler"/>.</summary>
    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            if (httpContext.Request.Headers.TryGetValue(HeaderName, out var correlationId))
            {
                if (!request.Headers.Contains(HeaderName))
                {
                    request.Headers.Add(HeaderName, correlationId.ToString());
                }
            }
            else if (httpContext.TraceIdentifier != null && !request.Headers.Contains(HeaderName))
            {
                request.Headers.Add(HeaderName, httpContext.TraceIdentifier);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
