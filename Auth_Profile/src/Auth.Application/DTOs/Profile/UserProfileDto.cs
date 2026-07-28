namespace Auth.Application.DTOs.Profile;

public record UserProfileDto(
    string Id,
    string Email,
    string Username,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AvatarUrl,
    bool IsEmailConfirmed,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
