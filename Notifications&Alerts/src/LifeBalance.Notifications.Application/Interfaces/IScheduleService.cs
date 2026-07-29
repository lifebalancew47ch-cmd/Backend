using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IScheduleService
{
    Task<NotificationResponseDto> ScheduleAsync(ScheduleRequestDto dto);
    Task<bool> CancelAsync(string id);
    Task<bool> RescheduleAsync(string id, DateTime newScheduledFor);
    Task<List<NotificationResponseDto>> GetScheduledAsync(string? userId = null);
}
