using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IAlertService
{
    Task<AlertDto> CreateAsync(CreateAlertDto dto);
    Task<List<AlertDto>> GetAllAsync(string userId);
    Task<AlertDto?> GetByIdAsync(string id);
    Task<bool> MarkAsReadAsync(string id);
    Task<bool> DismissAsync(string id);
}
