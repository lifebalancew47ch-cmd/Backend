namespace Auth.Application.DTOs.Auth;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
