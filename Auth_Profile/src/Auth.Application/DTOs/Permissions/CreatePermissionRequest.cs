namespace Auth.Application.DTOs.Permissions;

public record CreatePermissionRequest(string Name, string? Description, string Module);
