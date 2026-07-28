using Auth.Application.DTOs.Roles;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Queries.Roles;

public record GetAllRolesQuery() : IRequest<ApiResponse<IEnumerable<RoleDto>>>;
