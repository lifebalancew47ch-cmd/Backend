namespace Auth.Application.DTOs.Auth;

public record RefreshTokenRequest(string AccessToken, string RefreshToken);
