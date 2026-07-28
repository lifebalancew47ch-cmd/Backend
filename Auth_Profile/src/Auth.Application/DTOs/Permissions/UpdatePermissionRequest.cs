namespace Auth.Application.DTOs.Permissions;

public record UpdatePermissionRequest(string Name, string? Description, string Module);
