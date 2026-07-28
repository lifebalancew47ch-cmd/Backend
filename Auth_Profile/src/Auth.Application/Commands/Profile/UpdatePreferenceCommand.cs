using Auth.Application.DTOs.Profile;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Commands.Profile;

public record UpdatePreferenceCommand(UpdatePreferenceRequest Request, string UserId) : IRequest<ApiResponse<UserPreferenceDto>>;
