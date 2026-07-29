using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class MetricsService : IMetricsService
{
    private readonly MongoDbContext _db;
    public MetricsService(MongoDbContext db) { _db = db; }

    public async Task<MetricsDto> GetGeneralAsync()
    {
        var notifications = await _db.Notifications.Find(_ => true).ToListAsync();
        var logs = await _db.DeliveryLogs.Find(_ => true).ToListAsync();
        var total = notifications.LongCount();
        var delivered = notifications.LongCount(n => n.Status == NotificationStatus.Sent);
        var failed = notifications.LongCount(n => n.Status == NotificationStatus.Failed);
        var pending = notifications.LongCount(n => n.Status == NotificationStatus.Pending);
        var opened = logs.LongCount(l => l.OpenedAt.HasValue);
        var read = logs.LongCount(l => l.ReadAt.HasValue);
        var avgDelivery = logs.Where(l => l.DeliveryTimeMs.HasValue).Select(l => l.DeliveryTimeMs!.Value).DefaultIfEmpty().Average();
        var channelDist = notifications.GroupBy(n => n.Channel).ToDictionary(g => g.Key.ToString(), g => g.LongCount());
        var mostUsed = channelDist.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "N/A";
        return new MetricsDto { TotalSent = total, Delivered = delivered, Failed = failed, Pending = pending, Opened = opened, Read = read, Ctr = total > 0 ? (double)opened / total * 100 : 0, AverageDeliveryTimeMs = avgDelivery, MostUsedChannel = mostUsed, ChannelDistribution = channelDist };
    }

    public async Task<DeliveryMetricsDto> GetDeliveryAsync()
    {
        var logs = await _db.DeliveryLogs.Find(_ => true).ToListAsync();
        var total = logs.LongCount();
        var successful = logs.LongCount(l => l.Status == NotificationStatus.Sent);
        var failed = logs.LongCount(l => l.Status == NotificationStatus.Failed);
        var breakdown = logs.GroupBy(l => l.Status).ToDictionary(g => g.Key.ToString(), g => g.LongCount());
        var avgDelivery = logs.Where(l => l.DeliveryTimeMs.HasValue).Select(l => l.DeliveryTimeMs!.Value).DefaultIfEmpty().Average();
        return new DeliveryMetricsDto { TotalAttempts = total, SuccessfulDeliveries = successful, FailedDeliveries = failed, SuccessRate = total > 0 ? (double)successful / total * 100 : 0, AverageDeliveryTimeMs = avgDelivery, StatusBreakdown = breakdown };
    }

    public async Task<List<ChannelMetricsDto>> GetChannelsAsync()
    {
        var notifications = await _db.Notifications.Find(_ => true).ToListAsync();
        var total = notifications.LongCount();
        return notifications.GroupBy(n => n.Channel).Select(g => new ChannelMetricsDto { Channel = g.Key.ToString(), Count = g.LongCount(), Percentage = total > 0 ? (double)g.LongCount() / total * 100 : 0 }).OrderByDescending(c => c.Count).ToList();
    }

    public async Task<ErrorMetricsDto> GetErrorsAsync()
    {
        var errors = await _db.DeliveryLogs.Find(l => l.Status == NotificationStatus.Failed).ToListAsync();
        return new ErrorMetricsDto
        {
            TotalErrors = errors.LongCount(),
            RecentErrors = errors.OrderByDescending(l => l.CreatedAt).Take(10).Select(l => new ErrorDetail { NotificationId = l.NotificationId, Channel = l.Channel.ToString(), ErrorMessage = l.ErrorMessage ?? "Unknown", OccurredAt = l.CreatedAt }).ToList(),
            ErrorByType = errors.GroupBy(l => l.Channel.ToString()).ToDictionary(g => g.Key, g => g.LongCount())
        };
    }
}
