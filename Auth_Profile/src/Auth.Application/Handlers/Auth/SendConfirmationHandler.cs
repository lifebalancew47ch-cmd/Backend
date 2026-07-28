using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Auth;

public class SendConfirmationHandler : IRequestHandler<SendConfirmationCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendConfirmationHandler> _logger;

    public SendConfirmationHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository tokenRepository,
        IAuditService auditService,
        IEmailService emailService,
        ILogger<SendConfirmationHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _auditService = auditService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(SendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Request.Email, cancellationToken);

        if (user is null || user.IsEmailConfirmed)
            return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a confirmation link has been sent.");

        var token = Guid.NewGuid().ToString("N");
        var confirmationToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            Token = token,
            Email = user.Email,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        await _tokenRepository.AddAsync(confirmationToken, cancellationToken);

        await _emailService.SendEmailConfirmationEmailAsync(user.Email, token, cancellationToken);

        _logger.LogInformation("Email confirmation token generated for {Email}", user.Email);

        return ApiResponse<bool>.SuccessResponse(true, "Confirmation email sent.");
    }
}
