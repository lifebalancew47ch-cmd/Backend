using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationResponseDto> SendAsync(SendNotificationDto dto);
    Task<List<NotificationResponseDto>> SendBulkAsync(List<SendNotificationDto> dtos);
    Task<List<NotificationResponseDto>> BroadcastAsync(BroadcastNotificationDto dto);
    Task<NotificationResponseDto> ScheduleAsync(ScheduleNotificationDto dto);
    Task<List<NotificationResponseDto>> GetAllAsync(string? userId = null, string? organizationId = null, string? familyId = null, string? departmentId = null);
    Task<NotificationResponseDto?> GetByIdAsync(string id);
    Task<bool> CancelAsync(string id);
    Task<bool> MarkAsReadAsync(string id);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> ArchiveAsync(string id);
    Task<bool> FavoriteAsync(string id);
    Task<bool> DeleteAsync(string id);
}
