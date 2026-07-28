using Auth.Application.DTOs.Auth;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Auth;

public record RegisterCommand(RegisterRequest Request) : IRequest<ApiResponse<RegisterResponse>>;
