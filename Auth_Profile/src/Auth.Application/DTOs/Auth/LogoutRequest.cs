namespace Auth.Application.DTOs.Auth;

public record LogoutRequest(string? RefreshToken = null);
