namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record NotificationItemDto(string Id, string Title, string Message, string Severity, DateTime CreatedAtUtc, bool Read);

public interface INotificationServiceClient
{
    Task<List<NotificationItemDto>> GetUserNotificationsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default);
}
