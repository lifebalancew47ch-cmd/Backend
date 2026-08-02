using Auth.Application.Commands.Profile;
using Auth.Application.DTOs.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Profile;

public class UpdatePreferenceCommandHandler : IRequestHandler<UpdatePreferenceCommand, ApiResponse<UserPreferenceDto>>
{
    private readonly IUserPreferenceRepository _preferenceRepository;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdatePreferenceCommandHandler> _logger;

    public UpdatePreferenceCommandHandler(
        IUserPreferenceRepository preferenceRepository,
        IAuditService auditService,
        IMapper mapper,
        ILogger<UpdatePreferenceCommandHandler> logger)
    {
        _preferenceRepository = preferenceRepository;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserPreferenceDto>> Handle(UpdatePreferenceCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var preference = await _preferenceRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (preference is null)
        {
            preference = new UserPreference { UserId = request.UserId };
            await _preferenceRepository.AddAsync(preference, cancellationToken);
        }

        if (req.Theme is not null)
            preference.Theme = req.Theme;

        if (req.Language is not null)
            preference.Language = req.Language;

        if (req.Timezone is not null)
            preference.Timezone = req.Timezone;

        if (req.UnitsSystem is not null)
            preference.UnitsSystem = req.UnitsSystem;

        if (req.NotificationsEnabled.HasValue)
            preference.NotificationsEnabled = req.NotificationsEnabled.Value;

        if (req.EmailNotificationsEnabled.HasValue)
            preference.EmailNotificationsEnabled = req.EmailNotificationsEnabled.Value;

        if (req.PushNotificationsEnabled.HasValue)
            preference.PushNotificationsEnabled = req.PushNotificationsEnabled.Value;

        if (req.ProfileVisibility is not null)
            preference.ProfileVisibility = req.ProfileVisibility;

        if (req.MarketingConsent.HasValue)
            preference.MarketingConsent = req.MarketingConsent.Value;

        if (req.ActivitySharing.HasValue)
            preference.ActivitySharing = req.ActivitySharing.Value;

        preference.MarkUpdated();
        await _preferenceRepository.UpdateAsync(preference, cancellationToken);

        await _auditService.LogEventAsync(request.UserId, AuthEventType.ProfileUpdate,
            "Preferences updated", cancellationToken: cancellationToken);

        _logger.LogInformation("Preferences updated for user {UserId}", request.UserId);

        var dto = _mapper.Map<UserPreferenceDto>(preference);
        return ApiResponse<UserPreferenceDto>.SuccessResponse(dto);
    }
}
