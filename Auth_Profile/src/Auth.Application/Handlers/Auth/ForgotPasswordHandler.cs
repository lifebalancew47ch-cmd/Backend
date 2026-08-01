using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using Auth.Shared.Configurations;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Application.Handlers.Auth;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordHandler> _logger;
    private readonly SecuritySettings _securitySettings;

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IAuditService auditService,
        IEmailService emailService,
        ILogger<ForgotPasswordHandler> logger,
        IOptions<SecuritySettings> securitySettings)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _auditService = auditService;
        _emailService = emailService;
        _logger = logger;
        _securitySettings = securitySettings.Value;
    }

    public async Task<ApiResponse<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Request.Email, cancellationToken);

        if (user is not null)
        {
            await _tokenRepository.InvalidateExistingForUserAsync(user.Id, cancellationToken);

            var token = Guid.NewGuid().ToString("N");
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_securitySettings.PasswordResetTokenExpirationMinutes)
            };

            await _tokenRepository.AddAsync(resetToken, cancellationToken);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, token, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}. Token was saved; user can retry.", user.Email);
            }

            await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.PasswordReset,
                "Password reset requested", cancellationToken: cancellationToken);

            _logger.LogInformation("Password reset token generated for {Email}", user.Email);
        }

        return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a reset link has been sent.");
    }
}
