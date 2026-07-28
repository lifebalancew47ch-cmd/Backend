using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Auth;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IAuditService auditService,
        IEmailService emailService,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _auditService = auditService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Request.Email, cancellationToken);

        if (user is not null)
        {
            var token = Guid.NewGuid().ToString("N");
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            await _tokenRepository.AddAsync(resetToken, cancellationToken);

            await _emailService.SendPasswordResetEmailAsync(user.Email, token, cancellationToken);

            await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.PasswordReset,
                "Password reset requested", cancellationToken: cancellationToken);

            _logger.LogInformation("Password reset token generated for {Email}", user.Email);
        }

        return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a reset link has been sent.");
    }
}
