using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Auth;

public class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<ConfirmEmailHandler> _logger;

    public ConfirmEmailHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository tokenRepository,
        IAuditService auditService,
        ILogger<ConfirmEmailHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Request.Email, cancellationToken);

        if (user is null)
            return ApiResponse<bool>.FailResponse("Invalid confirmation request.");

        var confirmationToken = await _tokenRepository.GetByTokenAsync(request.Request.Token, cancellationToken);

        if (confirmationToken is null || !confirmationToken.IsValid || confirmationToken.UserId != user.Id)
            return ApiResponse<bool>.FailResponse("Invalid or expired confirmation token.");

        user.IsEmailConfirmed = true;
        await _userRepository.UpdateAsync(user, cancellationToken);

        confirmationToken.IsConfirmed = true;
        confirmationToken.ConfirmedAt = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(confirmationToken, cancellationToken);

        await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.EmailConfirmation,
            "Email confirmed", cancellationToken: cancellationToken);

        _logger.LogInformation("Email confirmed for {Email}", user.Email);

        return ApiResponse<bool>.SuccessResponse(true, "Email confirmed successfully.");
    }
}
