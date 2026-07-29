using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IPushService
{
    Task<NotificationResponseDto> SendAsync(SendPushDto dto);
    Task<List<NotificationResponseDto>> BroadcastAsync(BroadcastPushDto dto);
}
