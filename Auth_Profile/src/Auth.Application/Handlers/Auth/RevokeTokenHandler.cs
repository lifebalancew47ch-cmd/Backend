using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Auth;

public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, ApiResponse<bool>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<RevokeTokenHandler> _logger;

    public RevokeTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuditService auditService,
        ILogger<RevokeTokenHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.Request.RefreshToken, cancellationToken);

        if (refreshToken is null)
            return ApiResponse<bool>.FailResponse("Refresh token not found.");

        if (!refreshToken.IsActiveAndNotExpired)
            return ApiResponse<bool>.FailResponse("Refresh token is already revoked or expired.");

        refreshToken.IsActive = false;
        refreshToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        await _auditService.LogEventAsync(refreshToken.UserId, Domain.Enums.AuthEventType.TokenRevocation,
            "Refresh token revoked", cancellationToken: cancellationToken);

        _logger.LogInformation("Refresh token revoked for user {UserId}", refreshToken.UserId);

        return ApiResponse<bool>.SuccessResponse(true, "Token revoked successfully.");
    }
}
