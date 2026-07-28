using Auth.Application.DTOs.Roles;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Roles;

public record UpdateRoleCommand(string Id, UpdateRoleRequest Request) : IRequest<ApiResponse<RoleDto>>;
