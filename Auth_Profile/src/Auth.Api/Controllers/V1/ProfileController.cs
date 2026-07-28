using Auth.Application.Commands.Auth;
using Auth.Application.Commands.Profile;
using Auth.Application.Queries.Profile;
using Auth.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auth.Api.Controllers.V1;

public class ProfileController : BaseController
{
    [HttpGet("me", Name = "GetProfile")]
    [Authorize]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Profile.UserProfileDto>), 200)]
    [SwaggerOperation(Summary = "Get current user profile", Description = "Returns the authenticated user's profile.")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await Mediator.Send(new GetProfileQuery(userId), ct);
        return HandleResponse(result);
    }

    [HttpPut("me", Name = "UpdateProfile")]
    [Authorize]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Profile.UserProfileDto>), 200)]
    [SwaggerOperation(Summary = "Update current user profile", Description = "Updates the authenticated user's profile.")]
    public async Task<IActionResult> UpdateProfile([FromBody] Auth.Application.DTOs.Profile.UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await Mediator.Send(new UpdateProfileCommand(request, userId), ct);
        return HandleResponse(result);
    }

    [HttpGet("preferences", Name = "GetPreferences")]
    [Authorize]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Profile.UserPreferenceDto>), 200)]
    [SwaggerOperation(Summary = "Get user preferences", Description = "Returns the authenticated user's preferences.")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await Mediator.Send(new GetPreferencesQuery(userId), ct);
        return HandleResponse(result);
    }

    [HttpPut("preferences", Name = "UpdatePreferences")]
    [Authorize]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<Auth.Application.DTOs.Profile.UserPreferenceDto>), 200)]
    [SwaggerOperation(Summary = "Update user preferences", Description = "Updates the authenticated user's preferences.")]
    public async Task<IActionResult> UpdatePreferences([FromBody] Auth.Application.DTOs.Profile.UpdatePreferenceRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await Mediator.Send(new UpdatePreferenceCommand(request, userId), ct);
        return HandleResponse(result);
    }

    [HttpPut("change-password", Name = "ChangePassword")]
    [Authorize]
    [ProducesResponseType(typeof(Auth.Shared.Common.ApiResponse<bool>), 200)]
    [SwaggerOperation(Summary = "Change password", Description = "Changes the authenticated user's password.")]
    public async Task<IActionResult> ChangePassword([FromBody] Auth.Application.DTOs.Auth.ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await Mediator.Send(new ChangePasswordCommand(request, userId), ct);
        return HandleResponse(result);
    }
}
