using Auth.Application.DTOs.Permissions;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Queries.Permissions;

public record GetAllPermissionsQuery() : IRequest<ApiResponse<IEnumerable<PermissionDto>>>;
