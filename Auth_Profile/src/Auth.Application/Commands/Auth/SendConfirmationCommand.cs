using Auth.Application.DTOs.Auth;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Auth;

public record SendConfirmationCommand(SendConfirmationRequest Request) : IRequest<ApiResponse<bool>>;
