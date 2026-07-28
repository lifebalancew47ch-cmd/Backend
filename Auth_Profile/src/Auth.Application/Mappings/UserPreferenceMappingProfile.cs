using AutoMapper;
using Auth.Application.DTOs.Profile;
using Auth.Domain.Entities;

namespace Auth.Application.Mappings;

public class UserPreferenceMappingProfile : Profile
{
    public UserPreferenceMappingProfile()
    {
        CreateMap<UserPreference, UserPreferenceDto>()
            .ConstructUsing(src => new UserPreferenceDto(
                src.Theme,
                src.Language,
                src.Timezone,
                src.UnitsSystem,
                src.NotificationsEnabled,
                src.EmailNotificationsEnabled,
                src.PushNotificationsEnabled,
                src.ProfileVisibility,
                src.MarketingConsent,
                src.ActivitySharing));
    }
}
