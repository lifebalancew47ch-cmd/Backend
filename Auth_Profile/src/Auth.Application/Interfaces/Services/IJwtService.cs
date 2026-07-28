using System.Security.Claims;

namespace Auth.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    string GetJwtId(string token);
    DateTime GetAccessTokenExpiration();
}
