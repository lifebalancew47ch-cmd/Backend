using Auth.Application.DTOs.Auth;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Auth;

public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<ApiResponse<bool>>;
