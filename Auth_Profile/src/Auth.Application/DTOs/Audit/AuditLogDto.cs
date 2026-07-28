namespace Auth.Application.DTOs.Audit;

public record AuditLogDto(
    string Id,
    string? UserId,
    string Action,
    string? Details,
    string? IpAddress,
    string ResourceType,
    bool Success,
    string? ErrorMessage,
    DateTime CreatedAt);
