namespace LifeBalance.Notifications.Application.Interfaces;

public interface IDashboardServiceClient
{
    Task PushNotificationHistoryAsync(object historyData);
}
