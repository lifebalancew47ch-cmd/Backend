namespace Auth.Application.DTOs.Auth;

public record ConfirmEmailRequest(string Email, string Token);
