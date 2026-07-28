namespace Auth.Application.DTOs.Roles;

public record CreateRoleRequest(string Name, string? Description, List<string>? PermissionIds);
