using Auth.Application.DTOs.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Profile;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Profile;

public class GetPreferencesQueryHandler : IRequestHandler<GetPreferencesQuery, ApiResponse<UserPreferenceDto>>
{
    private readonly IUserPreferenceRepository _preferenceRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPreferencesQueryHandler> _logger;

    public GetPreferencesQueryHandler(
        IUserPreferenceRepository preferenceRepository,
        IMapper mapper,
        ILogger<GetPreferencesQueryHandler> logger)
    {
        _preferenceRepository = preferenceRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserPreferenceDto>> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        var preference = await _preferenceRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (preference is null)
        {
            preference = new UserPreference { UserId = request.UserId };
            await _preferenceRepository.AddAsync(preference, cancellationToken);

            _logger.LogInformation("Created default preferences for user {UserId}", request.UserId);
        }

        var dto = _mapper.Map<UserPreferenceDto>(preference);
        return ApiResponse<UserPreferenceDto>.SuccessResponse(dto);
    }
}
