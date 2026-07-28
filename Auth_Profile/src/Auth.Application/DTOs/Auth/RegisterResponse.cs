namespace Auth.Application.DTOs.Auth;

public record RegisterResponse(
    string UserId,
    string Email,
    string Username,
    bool RequiresEmailConfirmation);
