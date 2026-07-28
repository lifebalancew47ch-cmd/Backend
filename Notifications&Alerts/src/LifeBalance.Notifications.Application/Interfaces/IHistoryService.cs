using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Domain.Entities;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IHistoryService
{
    Task<List<NotificationHistoryDto>> GetAllAsync(string userId);
    Task<Notification?> GetByIdAsync(string id);
}
