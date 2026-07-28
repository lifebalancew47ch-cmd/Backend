using Auth.Application.DTOs.Auth;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Auth;

public record ChangePasswordCommand(ChangePasswordRequest Request, string UserId) : IRequest<ApiResponse<bool>>;
