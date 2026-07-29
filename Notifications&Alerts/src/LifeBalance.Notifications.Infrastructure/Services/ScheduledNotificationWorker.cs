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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledNotificationWorker> _logger;

    public ScheduledNotificationWorker(IServiceProvider serviceProvider, ILogger<ScheduledNotificationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledNotificationWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled notifications");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessScheduledNotifications(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var filter = Builders<ScheduledNotification>.Filter.And(
            Builders<ScheduledNotification>.Filter.Eq(s => s.IsActive, true),
            Builders<ScheduledNotification>.Filter.Lte(s => s.ScheduledFor, DateTime.UtcNow)
        );

        var dueNotifications = await db.ScheduledNotifications.Find(filter)
            .ToListAsync(cancellationToken);

        foreach (var scheduled in dueNotifications)
        {
            try
            {
                var dto = new SendNotificationDto
                {
                    UserId = scheduled.UserId,
                    Title = scheduled.Title,
                    Body = scheduled.Body,
                    Payload = scheduled.Payload,
                    Type = scheduled.Type,
                    Channel = scheduled.Channel
                };

                await notificationService.SendAsync(dto);

                if (scheduled.Recurrence == RecurrencePattern.None)
                {
                    await db.ScheduledNotifications.UpdateOneAsync(
                        Builders<ScheduledNotification>.Filter.Eq(s => s.Id, scheduled.Id),
                        Builders<ScheduledNotification>.Update.Set(s => s.IsActive, false)
                    );
                }
                else
                {
                    var nextRun = CalculateNextRun(scheduled);
                    await db.ScheduledNotifications.UpdateOneAsync(
                        Builders<ScheduledNotification>.Filter.Eq(s => s.Id, scheduled.Id),
                        Builders<ScheduledNotification>.Update.Set(s => s.ScheduledFor, nextRun)
                    );
                }

                _logger.LogInformation("Processed scheduled notification {Id}", scheduled.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process scheduled notification {Id}", scheduled.Id);
            }
        }
    }

    private static DateTime CalculateNextRun(ScheduledNotification scheduled)
    {
        var next = scheduled.ScheduledFor;

        switch (scheduled.Recurrence)
        {
            case RecurrencePattern.Daily:
                next = next.AddDays(1);
                break;
            case RecurrencePattern.Weekly:
                next = next.AddDays(7);
                break;
            case RecurrencePattern.Monthly:
                next = next.AddMonths(1);
                break;
            case RecurrencePattern.SpecificDate:
                next = scheduled.ScheduledFor.AddYears(1);
                break;
        }

        return next;
    }
}
