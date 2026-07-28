using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Permissions;

public record DeletePermissionCommand(string Id) : IRequest<ApiResponse<bool>>;
