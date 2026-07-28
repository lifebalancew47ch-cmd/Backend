using Auth.Application.DTOs.Roles;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Roles;

public record CreateRoleCommand(CreateRoleRequest Request) : IRequest<ApiResponse<RoleDto>>;
