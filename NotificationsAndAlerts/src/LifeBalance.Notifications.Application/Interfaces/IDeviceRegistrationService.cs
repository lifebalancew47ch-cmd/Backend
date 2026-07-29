using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IDeviceRegistrationService
{
    Task RegisterAsync(DeviceRegistrationDto dto);
    Task<List<string>> GetDeviceTokensAsync(string userId);
    Task<bool> UnregisterAsync(string userId, string deviceToken);
}
