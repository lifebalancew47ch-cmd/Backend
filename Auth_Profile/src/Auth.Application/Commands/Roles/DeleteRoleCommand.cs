using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Roles;

public record DeleteRoleCommand(string Id) : IRequest<ApiResponse<bool>>;
