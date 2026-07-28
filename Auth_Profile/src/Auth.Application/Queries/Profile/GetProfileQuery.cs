using Auth.Application.DTOs.Profile;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Queries.Profile;

public record GetProfileQuery(string UserId) : IRequest<ApiResponse<UserProfileDto>>;
