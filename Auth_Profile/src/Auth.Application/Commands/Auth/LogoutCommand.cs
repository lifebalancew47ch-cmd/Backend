using Auth.Application.DTOs.Auth;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Auth;

public record LogoutCommand(LogoutRequest Request, string? UserId = null) : IRequest<ApiResponse<bool>>;
