using Auth.Application.DTOs.Profile;

namespace Auth.Application.DTOs.Auth;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileDto UserProfile);
