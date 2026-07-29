using LifeBalance.Notifications.Application.DTOs;

namespace LifeBalance.Notifications.Application.Interfaces;

public interface IEmailService
{
    Task<NotificationResponseDto> SendAsync(SendEmailDto dto);
    Task<NotificationResponseDto> SendTemplateAsync(EmailTemplateDto dto);
    Task<List<NotificationResponseDto>> SendBulkAsync(BulkEmailDto dto);
}
