using Auth.Application.DTOs.Profile;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Queries.Profile;

public record GetPreferencesQuery(string UserId) : IRequest<ApiResponse<UserPreferenceDto>>;
