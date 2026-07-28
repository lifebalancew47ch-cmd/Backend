using Microsoft.AspNetCore.Http;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients.DelegatingHandlers;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

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
