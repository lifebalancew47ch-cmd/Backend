namespace LifeBalance.Notifications.Application.DTOs;

public record NotificationItemDto(
    string Id,
    string Title,
    string Message,
    string Severity,
    DateTime CreatedAtUtc,
    bool Read);
