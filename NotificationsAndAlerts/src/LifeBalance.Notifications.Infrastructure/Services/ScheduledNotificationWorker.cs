using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Entities;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class ScheduledNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ScheduledNotificationWorker> _logger;
    public ScheduledNotificationWorker(IServiceProvider sp, ILogger<ScheduledNotificationWorker> logger) { _sp = sp; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ScheduledNotificationWorker started");
        while (!ct.IsCancellationRequested)
        {
            try { await Process(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error processing scheduled"); }
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }

    private async Task Process(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var ns = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var due = await db.ScheduledNotifications.Find(s => s.IsActive && s.ScheduledFor <= DateTime.UtcNow).ToListAsync(ct);
        foreach (var s in due)
        {
            try
            {
                await ns.SendAsync(new SendNotificationDto { UserId = s.UserId, Title = s.Title, Body = s.Body, Payload = s.Payload, Type = s.Type, Channel = s.Channel });
                if (s.Recurrence == RecurrencePattern.None)
                    await db.ScheduledNotifications.UpdateOneAsync(x => x.Id == s.Id, Builders<ScheduledNotification>.Update.Set(x => x.IsActive, false));
                else
                    await db.ScheduledNotifications.UpdateOneAsync(x => x.Id == s.Id, Builders<ScheduledNotification>.Update.Set(x => x.ScheduledFor, NextRun(s)));
                _logger.LogInformation("Processed scheduled {Id}", s.Id);
            }
            catch (Exception ex) { _logger.LogError(ex, "Failed scheduled {Id}", s.Id); }
        }
    }

    private static DateTime NextRun(ScheduledNotification s) => s.Recurrence switch
    {
        RecurrencePattern.Daily => s.ScheduledFor.AddDays(1),
        RecurrencePattern.Weekly => s.ScheduledFor.AddDays(7),
        RecurrencePattern.Monthly => s.ScheduledFor.AddMonths(1),
        _ => s.ScheduledFor.AddYears(1)
    };
}
