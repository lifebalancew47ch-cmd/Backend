using Auth.Application.DTOs.Permissions;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Permissions;

public record CreatePermissionCommand(CreatePermissionRequest Request) : IRequest<ApiResponse<PermissionDto>>;
