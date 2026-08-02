using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Application.Interfaces.Services;
using Auth.Shared.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jti = Guid.NewGuid().ToString();
        var allClaims = claims.Select(c => MapOutboundClaimType(c)).ToList();
        allClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));
        allClaims.Add(new Claim(JwtRegisteredClaimNames.Iat,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: allClaims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Serializa los claims con los nombres estándar de JWT (sub, email, name, role)
    /// en lugar de los URIs largos de ClaimTypes.*, para que los consumidores
    /// (Dashboard, Notifications, Organization, frontend) los mapeen correctamente
    /// y las políticas RequireRole funcionen (ej. "role": "USER").
    /// </summary>
    private static Claim MapOutboundClaimType(Claim claim)
    {
        return claim.Type switch
        {
            ClaimTypes.NameIdentifier => new Claim(JwtRegisteredClaimNames.Sub, claim.Value, claim.ValueType),
            ClaimTypes.Email => new Claim(JwtRegisteredClaimNames.Email, claim.Value, claim.ValueType),
            ClaimTypes.Name => new Claim(JwtRegisteredClaimNames.Name, claim.Value, claim.ValueType),
            ClaimTypes.Role => new Claim("role", claim.Value, claim.ValueType),
            _ => claim
        };
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }

    public string GetJwtId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
    }

    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
    }
}
