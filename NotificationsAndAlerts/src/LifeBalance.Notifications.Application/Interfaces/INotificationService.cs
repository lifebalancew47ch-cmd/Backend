using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationResponseDto> SendAsync(SendNotificationDto dto);
    Task<List<NotificationResponseDto>> BroadcastAsync(BroadcastNotificationDto dto);
}
