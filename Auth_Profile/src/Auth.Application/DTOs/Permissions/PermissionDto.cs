namespace Auth.Application.DTOs.Permissions;

public record PermissionDto(
    string Id,
    string Name,
    string? Description,
    string Module,
    DateTime CreatedAt);
