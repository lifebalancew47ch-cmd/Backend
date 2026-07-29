using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IScheduleService
{
    Task<NotificationResponseDto> ScheduleAsync(ScheduleNotificationDto dto);
    Task<bool> CancelAsync(string id);
}
