using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IPreferenceService
{
    Task<NotificationPreferenceDto> GetAsync(string userId);
    Task<NotificationPreferenceDto> UpdateAsync(string userId, UpdatePreferenceDto dto);
}
