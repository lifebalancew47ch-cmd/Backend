namespace Auth.Application.DTOs.Roles;

public record RoleDto(
    string Id,
    string Name,
    string? Description,
    List<string> PermissionIds,
    DateTime CreatedAt);
