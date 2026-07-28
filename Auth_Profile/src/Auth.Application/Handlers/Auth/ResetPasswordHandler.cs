using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Auth;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordService passwordService,
        IAuditService auditService,
        ILogger<ResetPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordService = passwordService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var user = await _userRepository.GetByEmailAsync(req.Email, cancellationToken);

        if (user is null)
            return ApiResponse<bool>.FailResponse("Invalid reset request.");

        var resetToken = await _tokenRepository.GetByTokenAsync(req.Token, cancellationToken);

        if (resetToken is null || !resetToken.IsValid || resetToken.UserId != user.Id)
            return ApiResponse<bool>.FailResponse("Invalid or expired reset token.");

        user.PasswordHash = _passwordService.HashPassword(req.NewPassword);
        user.LastPasswordChangeAt = DateTime.UtcNow;
        user.ResetFailedLoginAttempts();
        await _userRepository.UpdateAsync(user, cancellationToken);

        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(resetToken, cancellationToken);

        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken: cancellationToken);

        await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.PasswordReset,
            "Password reset completed", cancellationToken: cancellationToken);

        _logger.LogInformation("Password reset completed for {Email}", user.Email);

        return ApiResponse<bool>.SuccessResponse(true, "Password reset successful.");
    }
}
