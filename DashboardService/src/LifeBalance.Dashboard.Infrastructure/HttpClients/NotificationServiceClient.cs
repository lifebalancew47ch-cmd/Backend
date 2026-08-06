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

    public async Task<List<NotificationItemDto>?> GetUserNotificationsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 100);
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<NotificationPayloadDto>>($"/api/v1/notifications/user?limit={clampedLimit}", cancellationToken);
            return items?
                .Select(n => new NotificationItemDto(
                    n.Id ?? string.Empty,
                    n.Title ?? string.Empty,
                    n.Body ?? string.Empty,
                    n.Type ?? string.Empty,
                    n.CreatedAt,
                    n.IsRead))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve notifications for UserId: {UserId}", userId);
            return null;
        }
    }

    private sealed class NotificationPayloadDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
