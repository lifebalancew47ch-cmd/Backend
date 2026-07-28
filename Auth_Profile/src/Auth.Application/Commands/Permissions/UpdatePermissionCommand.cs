using Auth.Application.DTOs.Permissions;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Permissions;

public record UpdatePermissionCommand(string Id, UpdatePermissionRequest Request) : IRequest<ApiResponse<PermissionDto>>;
