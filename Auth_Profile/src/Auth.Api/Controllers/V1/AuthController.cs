using Auth.Application.Commands.Auth;
using Auth.Api.Extensions;
using Auth.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auth.Api.Controllers.V1;

public class AuthController : BaseController
{
    [HttpPost("register", Name = "Register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Auth.RegisterResponse>), 200)]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Auth.RegisterResponse>), 400)]
    [SwaggerOperation(Summary = "Register a new user", Description = "Creates a new user account.")]
    public async Task<IActionResult> Register([FromBody] Auth.Application.DTOs.Auth.RegisterRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new RegisterCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPost("login", Name = "Login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Auth.LoginResponse>), 200)]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Auth.LoginResponse>), 401)]
    [SwaggerOperation(Summary = "User login", Description = "Authenticates a user and returns JWT tokens.")]
    public async Task<IActionResult> Login([FromBody] Auth.Application.DTOs.Auth.LoginRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var loginRequest = request with { IpAddress = ipAddress };
        var result = await Mediator.Send(new LoginCommand(loginRequest), ct);
        return HandleResponse(result);
    }

    [HttpPost("logout", Name = "Logout")]
    [Authorize]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [SwaggerOperation(Summary = "User logout", Description = "Revokes the refresh token and logs out the user.")]
    public async Task<IActionResult> Logout([FromBody] Auth.Application.DTOs.Auth.LogoutRequest? request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await Mediator.Send(new LogoutCommand(request ?? new Auth.Application.DTOs.Auth.LogoutRequest(), userId), ct);
        return HandleResponse(result);
    }

    [HttpPost("refresh-token", Name = "RefreshToken")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Auth.RefreshTokenResponse>), 200)]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Auth.RefreshTokenResponse>), 401)]
    [SwaggerOperation(Summary = "Refresh access token", Description = "Generates a new access token using a refresh token.")]
    public async Task<IActionResult> RefreshToken([FromBody] Auth.Application.DTOs.Auth.RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new RefreshTokenCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPost("revoke-token", Name = "RevokeToken")]
    [Authorize]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [SwaggerOperation(Summary = "Revoke a refresh token", Description = "Revokes a specific refresh token.")]
    public async Task<IActionResult> RevokeToken([FromBody] Auth.Application.DTOs.Auth.TokenRevocationRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new RevokeTokenCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPost("forgot-password", Name = "ForgotPassword")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [SwaggerOperation(Summary = "Request password reset", Description = "Sends a password reset email if the account exists.")]
    public async Task<IActionResult> ForgotPassword([FromBody] Auth.Application.DTOs.Auth.ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new ForgotPasswordCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPost("reset-password", Name = "ResetPassword")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 400)]
    [SwaggerOperation(Summary = "Reset password", Description = "Resets user password using the token from email.")]
    public async Task<IActionResult> ResetPassword([FromBody] Auth.Application.DTOs.Auth.ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new ResetPasswordCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPost("send-confirmation", Name = "SendConfirmation")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [SwaggerOperation(Summary = "Send email confirmation", Description = "Sends an email confirmation link.")]
    public async Task<IActionResult> SendConfirmation([FromBody] Auth.Application.DTOs.Auth.SendConfirmationRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SendConfirmationCommand(request), ct);
        return HandleResponse(result);
    }

    [HttpPost("confirm-email", Name = "ConfirmEmail")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 400)]
    [SwaggerOperation(Summary = "Confirm email", Description = "Confirms user email using the token from email.")]
    public async Task<IActionResult> ConfirmEmail([FromBody] Auth.Application.DTOs.Auth.ConfirmEmailRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new ConfirmEmailCommand(request), ct);
        return HandleResponse(result);
    }
}
