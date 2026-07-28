using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Auth;

public class LogoutHandler : IRequestHandler<LogoutCommand, ApiResponse<bool>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuditService auditService,
        ILogger<LogoutHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Request.RefreshToken))
        {
            var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.Request.RefreshToken, cancellationToken);

            if (existingToken is not null)
            {
                existingToken.IsActive = false;
                existingToken.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

                await _auditService.LogEventAsync(existingToken.UserId, Domain.Enums.AuthEventType.Logout,
                    "Refresh token revoked on logout", cancellationToken: cancellationToken);
            }
        }

        _logger.LogInformation("User logged out");
        return ApiResponse<bool>.SuccessResponse(true, "Logout successful.");
    }
}
