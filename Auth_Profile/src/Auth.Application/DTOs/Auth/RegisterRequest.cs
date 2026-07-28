namespace Auth.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    string? PhoneNumber = null);
