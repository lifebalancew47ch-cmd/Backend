namespace Auth.Application.DTOs.Audit;

public record LoginHistoryDto(
    string Id,
    string Email,
    string IpAddress,
    string? UserAgent,
    string? Device,
    bool Success,
    string? FailureReason,
    DateTime LoginAt);
