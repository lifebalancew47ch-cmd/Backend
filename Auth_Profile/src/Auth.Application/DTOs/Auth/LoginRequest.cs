namespace Auth.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password, string? IpAddress = null);
