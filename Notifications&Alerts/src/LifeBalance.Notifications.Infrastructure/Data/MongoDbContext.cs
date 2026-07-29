using LifeBalance.Notifications.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace LifeBalance.Notifications.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(configuration["DatabaseName"] ?? "LifeBalanceNotificationsDb");
    }

    public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("notifications");
    public IMongoCollection<NotificationPreference> NotificationPreferences => _database.GetCollection<NotificationPreference>("notification_preferences");
    public IMongoCollection<NotificationTemplate> NotificationTemplates => _database.GetCollection<NotificationTemplate>("notification_templates");
    public IMongoCollection<ScheduledNotification> ScheduledNotifications => _database.GetCollection<ScheduledNotification>("scheduled_notifications");
    public IMongoCollection<DeliveryLog> DeliveryLogs => _database.GetCollection<DeliveryLog>("delivery_logs");
    public IMongoCollection<Alert> Alerts => _database.GetCollection<Alert>("alerts");
    public IMongoCollection<MetricsRecord> MetricsRecords => _database.GetCollection<MetricsRecord>("metrics_records");
}
