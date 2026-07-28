using Auth.Application.Commands.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Auth;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IAuditService auditService,
        ILogger<ChangePasswordHandler> logger)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return ApiResponse<bool>.FailResponse("User not found.");

        if (!_passwordService.VerifyPassword(request.Request.CurrentPassword, user.PasswordHash))
            return ApiResponse<bool>.FailResponse("Current password is incorrect.");

        user.PasswordHash = _passwordService.HashPassword(request.Request.NewPassword);
        user.LastPasswordChangeAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.PasswordChange,
            "Password changed", cancellationToken: cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}", user.Id);

        return ApiResponse<bool>.SuccessResponse(true, "Password changed successfully.");
    }
}
