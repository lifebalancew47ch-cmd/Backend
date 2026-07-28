using System.Net.Http.Json;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeBalance.Dashboard.Infrastructure.HttpClients;

public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<NotificationItemDto>> GetUserNotificationsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<NotificationItemDto>>($"/api/v1/notifications/user/{userId}?limit={limit}", cancellationToken);
            return res ?? GetFallbackNotifications();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve notifications for UserId: {UserId}", userId);
            return GetFallbackNotifications();
        }
    }

    private static List<NotificationItemDto> GetFallbackNotifications() => new()
    {
        new NotificationItemDto("notif_1", "Sedentary Alert", "Time to stand up and stretch!", "Warning", DateTime.UtcNow.AddMinutes(-30), false),
        new NotificationItemDto("notif_2", "Goal Achieved", "You completed your daily step goal!", "Info", DateTime.UtcNow.AddHours(-2), true)
    };
}
