using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using Auth.Shared.Configurations;
using Auth.Shared.Enums;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Application.Handlers.Auth;

public class RegisterHandler : IRequestHandler<RegisterCommand, ApiResponse<RegisterResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly IEmailConfirmationTokenRepository _emailConfirmationTokenRepository;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly SecuritySettings _securitySettings;

    public RegisterHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordService passwordService,
        IAuditService auditService,
        IEmailConfirmationTokenRepository emailConfirmationTokenRepository,
        IEmailService emailService,
        IMapper mapper,
        ILogger<RegisterHandler> logger,
        IOptions<SecuritySettings> securitySettings)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordService = passwordService;
        _auditService = auditService;
        _emailConfirmationTokenRepository = emailConfirmationTokenRepository;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
        _securitySettings = securitySettings.Value;
    }

    public async Task<ApiResponse<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _userRepository.ExistsByEmailAsync(req.Email, cancellationToken: cancellationToken))
            return ApiResponse<RegisterResponse>.FailResponse("Email is already registered.");

        if (await _userRepository.ExistsByUsernameAsync(req.Username, cancellationToken: cancellationToken))
            return ApiResponse<RegisterResponse>.FailResponse("Username is already taken.");

        var defaultRole = await _roleRepository.GetByNameAsync("User", cancellationToken)
            ?? await _roleRepository.GetByNameAsync("USER", cancellationToken);

        var user = new User
        {
            Email = req.Email.ToLowerInvariant().Trim(),
            Username = req.Username.Trim(),
            PasswordHash = _passwordService.HashPassword(req.Password),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            PhoneNumber = req.PhoneNumber?.Trim(),
            IsActive = true,
            IsEmailConfirmed = false,
            RoleIds = defaultRole is not null ? new List<string> { defaultRole.Id } : new List<string>()
        };

        await _userRepository.AddAsync(user, cancellationToken);

        await _auditService.LogEventAsync(
            user.Id, Domain.Enums.AuthEventType.Register,
            $"User registered: {user.Email}",
            cancellationToken: cancellationToken);

        var token = Guid.NewGuid().ToString("N");
        var confirmationToken = new EmailConfirmationToken
        {
            UserId = user.Id,
            Token = token,
            Email = user.Email,
            ExpiresAt = DateTime.UtcNow.AddHours(_securitySettings.EmailConfirmationTokenExpirationHours)
        };

        await _emailConfirmationTokenRepository.AddAsync(confirmationToken, cancellationToken);
        await _emailService.SendEmailConfirmationEmailAsync(user.Email, token, cancellationToken);

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        var response = new RegisterResponse(user.Id, user.Email, user.Username, true);
        return ApiResponse<RegisterResponse>.SuccessResponse(response, "Registration successful. Please confirm your email.");
    }
}
