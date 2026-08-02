using Microsoft.AspNetCore.Http;

namespace LifeBalance.Administration.Infrastructure.ExternalServices;

/// <summary>
/// Propagates the caller's JWT bearer token to outbound upstream calls so
/// services behind authentication (Auth, Organization, ...) accept the request.
/// The original token is ALWAYS taken from the inbound request (never minted
/// here), so downstream authorization keeps the original identity.
/// </summary>
public class BearerTokenPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenPropagationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
