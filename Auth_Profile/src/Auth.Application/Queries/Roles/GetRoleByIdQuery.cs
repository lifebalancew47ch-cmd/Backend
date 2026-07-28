using Auth.Application.DTOs.Roles;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Queries.Roles;

public record GetRoleByIdQuery(string Id) : IRequest<ApiResponse<RoleDto>>;
