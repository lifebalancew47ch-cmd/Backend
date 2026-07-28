using Auth.Application.Commands.Auth;
using Auth.Application.DTOs.Auth;
using Auth.Application.DTOs.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using Auth.Shared.Enums;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Auth.Application.Handlers.Auth;

public class LoginHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IPasswordService passwordService,
        IAuditService auditService,
        ILoginHistoryRepository loginHistoryRepository,
        IMapper mapper,
        ILogger<LoginHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _auditService = auditService;
        _loginHistoryRepository = loginHistoryRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var user = await _userRepository.GetByEmailAsync(req.Email, cancellationToken);

        if (user is null)
        {
            await LogFailedLogin(req.Email, "User not found", req.IpAddress, cancellationToken);
            return ApiResponse<LoginResponse>.FailResponse("Invalid email or password.");
        }

        if (user.IsLockedOut)
        {
            await LogFailedLogin(req.Email, "Account locked", req.IpAddress, cancellationToken);
            return ApiResponse<LoginResponse>.FailResponse("Account is locked. Please try again later.");
        }

        if (!user.IsActive)
        {
            await LogFailedLogin(req.Email, "Account inactive", req.IpAddress, cancellationToken);
            return ApiResponse<LoginResponse>.FailResponse("Account is inactive.");
        }

        if (!_passwordService.VerifyPassword(req.Password, user.PasswordHash))
        {
            user.IncrementFailedLoginAttempts();

            if (user.FailedLoginAttempts >= 5)
            {
                user.LockOut(TimeSpan.FromMinutes(15));
                await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.AccountLockout,
                    "Account locked due to too many failed attempts", req.IpAddress,
                    cancellationToken: cancellationToken);
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            await LogFailedLogin(req.Email, "Invalid password", req.IpAddress, cancellationToken);

            return ApiResponse<LoginResponse>.FailResponse("Invalid email or password.");
        }

        user.ResetFailedLoginAttempts();
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var roles = await _roleRepository.GetByIdsAsync(user.RoleIds, cancellationToken);
        var roleNames = roles.Select(r => r.NormalizedName).ToList();

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
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var accessToken = _jwtService.GenerateAccessToken(claims);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            JwtId = _jwtService.GetJwtId(accessToken),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = req.IpAddress ?? "unknown"
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await _loginHistoryRepository.AddAsync(new LoginHistory
        {
            UserId = user.Id,
            Email = user.Email,
            IpAddress = req.IpAddress ?? "unknown",
            Success = true,
            LoginAt = DateTime.UtcNow
        }, cancellationToken);

        await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.Login,
            "Login successful", req.IpAddress, cancellationToken: cancellationToken);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        var userProfile = _mapper.Map<UserProfileDto>(user);
        var expiresAt = _jwtService.GetAccessTokenExpiration();

        var response = new LoginResponse(accessToken, refreshTokenValue, expiresAt, userProfile);
        return ApiResponse<LoginResponse>.SuccessResponse(response, "Login successful.");
    }

    private async Task LogFailedLogin(string email, string reason, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        await _loginHistoryRepository.AddAsync(new LoginHistory
        {
            UserId = user?.Id ?? string.Empty,
            Email = email,
            IpAddress = ipAddress ?? "unknown",
            Success = false,
            FailureReason = reason,
            LoginAt = DateTime.UtcNow
        }, cancellationToken);

        if (user != null)
        {
            await _auditService.LogEventAsync(user.Id, Domain.Enums.AuthEventType.FailedLogin,
                $"Failed login: {reason}", ipAddress, cancellationToken: cancellationToken);
        }

        _logger.LogWarning("Failed login attempt for {Email}: {Reason}", email, reason);
    }
}
