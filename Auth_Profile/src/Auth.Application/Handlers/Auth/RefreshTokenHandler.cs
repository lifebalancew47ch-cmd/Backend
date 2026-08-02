using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using Auth.Shared.Configurations;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Auth.Application.Handlers.Auth;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<RefreshTokenResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtService _jwtService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RefreshTokenHandler> _logger;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IJwtService jwtService,
        IAuditService auditService,
        ILogger<RefreshTokenHandler> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtService = jwtService;
        _auditService = auditService;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<ApiResponse<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.Request.AccessToken);
        var jwtId = principal?.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;

        if (jwtId is null)
            return ApiResponse<RefreshTokenResponse>.FailResponse("Invalid access token.");

        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.Request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            _logger.LogWarning("Refresh token not found: possible token hijacking attempt");
            return ApiResponse<RefreshTokenResponse>.FailResponse("Invalid refresh token.");
        }

        if (!refreshToken.IsActiveAndNotExpired)
            return ApiResponse<RefreshTokenResponse>.FailResponse("Refresh token is no longer valid.");

        if (refreshToken.JwtId != jwtId)
        {
            _logger.LogWarning("Refresh token JWT ID mismatch for user {UserId}: possible token reuse attack", refreshToken.UserId);
            await _refreshTokenRepository.RevokeAllByUserIdAsync(refreshToken.UserId, cancellationToken: cancellationToken);
            return ApiResponse<RefreshTokenResponse>.FailResponse("Token reuse detected. All sessions have been revoked.");
        }

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return ApiResponse<RefreshTokenResponse>.FailResponse("User account is not available.");

        var roles = await _roleRepository.GetByIdsAsync(user.RoleIds, cancellationToken);
        var roleNames = roles.Select(r => r.NormalizedName).ToList();

        if (roleNames.Count == 0)
        {
            var defaultRole = await _roleRepository.GetByNameAsync("User", cancellationToken)
                ?? await _roleRepository.GetByNameAsync("USER", cancellationToken);
            if (defaultRole is not null)
            {
                roleNames.Add(defaultRole.NormalizedName);
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("isEmailConfirmed", user.IsEmailConfirmed.ToString())
        };

        foreach (var roleName in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, roleName));

        var newAccessToken = _jwtService.GenerateAccessToken(claims);
        var newRefreshTokenValue = _jwtService.GenerateRefreshToken();

        refreshToken.IsActive = false;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = newRefreshTokenValue;
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenValue,
            JwtId = _jwtService.GetJwtId(newAccessToken),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedByIp = refreshToken.CreatedByIp
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.TokenRefresh,
            "Access token refreshed", cancellationToken: cancellationToken);

        var expiresAt = _jwtService.GetAccessTokenExpiration();

        return ApiResponse<RefreshTokenResponse>.SuccessResponse(
            new RefreshTokenResponse(newAccessToken, newRefreshTokenValue, expiresAt),
            "Token refreshed successfully.");
    }
}
