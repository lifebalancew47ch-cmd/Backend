using Auth.Application.DTOs.Auth;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Auth;

public record RevokeTokenCommand(TokenRevocationRequest Request, string UserId) : IRequest<ApiResponse<bool>>;
