namespace Auth.Application.DTOs.Roles;

public record UpdateRoleRequest(string Name, string? Description, List<string>? PermissionIds);
