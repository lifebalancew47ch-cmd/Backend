using Auth.Application.DTOs.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Profile;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Profile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ApiResponse<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProfileQueryHandler> _logger;

    public GetProfileQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<GetProfileQueryHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return ApiResponse<UserProfileDto>.FailResponse("User not found.", statusCode: 404);

        _logger.LogInformation("Profile retrieved for user {UserId}", user.Id);

        var profile = _mapper.Map<UserProfileDto>(user);
        return ApiResponse<UserProfileDto>.SuccessResponse(profile);
    }
}
