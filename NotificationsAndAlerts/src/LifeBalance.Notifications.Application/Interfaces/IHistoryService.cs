using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IHistoryService
{
    Task<PaginatedResult<NotificationHistoryDto>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<NotificationHistoryDto>> GetByUserAsync(string userId);
    Task<List<NotificationHistoryDto>> GetByOrganizationAsync(string organizationId);
    Task<NotificationResponseDto?> GetByIdAsync(string id);
}
