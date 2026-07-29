using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class PushService : IPushService
{
    private readonly MongoDbContext _db;
    private readonly IPushNotificationProvider _pushProvider;
    private readonly IDeviceRegistrationService _deviceService;
    private readonly IPreferenceService _preferenceService;
    private readonly ILogger<PushService> _logger;

    public PushService(
        MongoDbContext db,
        IPushNotificationProvider pushProvider,
        IDeviceRegistrationService deviceService,
        IPreferenceService preferenceService,
        ILogger<PushService> logger)
    {
        _db = db;
        _pushProvider = pushProvider;
        _deviceService = deviceService;
        _preferenceService = preferenceService;
        _logger = logger;
    }

    public async Task<NotificationResponseDto> SendAsync(SendPushDto dto)
    {
        var channel = dto.Platform == DevicePlatform.WearOS ? NotificationChannel.WearOS : NotificationChannel.Push;

        var prefs = await _preferenceService.GetAsync(dto.UserId);
        if (!prefs.ReceivePush || (channel == NotificationChannel.WearOS && !prefs.ReceiveWearOS))
        {
            _logger.LogInformation("User {UserId} has disabled {Channel}", dto.UserId, channel);
            return await Save(dto.UserId, dto.Title, dto.Body, dto.Payload, channel,
                NotificationStatus.Cancelled, "User has disabled this channel", 0);
        }

        var deviceTokens = dto.DeviceTokens;
        if (deviceTokens.Count == 0)
            deviceTokens = await _deviceService.GetDeviceTokensAsync(dto.UserId);

        if (deviceTokens.Count == 0)
        {
            _logger.LogWarning("No device tokens for user {UserId}", dto.UserId);
            return await Save(dto.UserId, dto.Title, dto.Body, dto.Payload, channel,
                NotificationStatus.Failed, "No device tokens registered", 0);
        }

        var results = await _pushProvider.SendToDevicesAsync(deviceTokens, dto.Title, dto.Body, dto.Payload);
        var allSucceeded = results.All(r => r.Success);
        var anySucceeded = results.Any(r => r.Success);
        var firstError = results.FirstOrDefault(r => !r.Success)?.ErrorMessage;

        var status = allSucceeded ? NotificationStatus.Sent
                    : anySucceeded ? NotificationStatus.Sent
                    : NotificationStatus.Failed;

        return await Save(dto.UserId, dto.Title, dto.Body, dto.Payload, channel,
            status, allSucceeded ? null : firstError ?? "All delivery attempts failed", deviceTokens.Count);
    }

    public async Task<List<NotificationResponseDto>> BroadcastAsync(BroadcastPushDto dto)
    {
        var userIds = dto.UserIds ?? new List<string>();
        // Note: In production, resolve org/family/department members via IOrganizationServiceClient
        userIds = userIds.Distinct().ToList();
        var results = new List<NotificationResponseDto>();

        foreach (var userId in userIds)
        {
            var result = await SendAsync(new SendPushDto
            {
                UserId = userId,
                Title = dto.Title,
                Body = dto.Body,
                Payload = dto.Payload,
                Platform = dto.Platform
            });
            results.Add(result);
        }
        return results;
    }

    private async Task<NotificationResponseDto> Save(string userId, string title, string body, string? payload,
        NotificationChannel channel, NotificationStatus status, string? errorMessage, int deviceCount)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Payload = payload,
            Type = NotificationType.Information,
            Channel = channel,
            Status = status,
            Attempts = deviceCount > 0 ? 1 : 0,
            ErrorMessage = errorMessage,
            Provider = "Firebase",
            CreatedAt = DateTime.UtcNow,
            SentAt = status == NotificationStatus.Sent ? DateTime.UtcNow : null
        };

        await _db.Notifications.InsertOneAsync(notification);
        await _db.DeliveryLogs.InsertOneAsync(new DeliveryLog
        {
            NotificationId = notification.Id,
            UserId = userId,
            Channel = channel,
            Status = status,
            Attempts = notification.Attempts,
            ErrorMessage = errorMessage,
            Provider = "Firebase",
            CreatedAt = DateTime.UtcNow
        });

        return MapToResponse(notification);
    }

    private static NotificationResponseDto MapToResponse(Notification n) => new()
    {
        Id = n.Id, UserId = n.UserId, OrganizationId = n.OrganizationId,
        FamilyId = n.FamilyId, DepartmentId = n.DepartmentId,
        Title = n.Title, Body = n.Body, Payload = n.Payload,
        Type = n.Type, Channel = n.Channel, Status = n.Status,
        IsRead = n.IsRead, IsArchived = n.IsArchived, IsFavorite = n.IsFavorite,
        CreatedAt = n.CreatedAt, SentAt = n.SentAt, ReadAt = n.ReadAt,
        DeliveryTimeMs = n.DeliveryTimeMs, Attempts = n.Attempts,
        ErrorMessage = n.ErrorMessage, Provider = n.Provider
    };
}
