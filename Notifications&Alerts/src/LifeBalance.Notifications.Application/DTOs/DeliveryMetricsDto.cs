namespace LifeBalance.Notifications.Application.DTOs;

public class DeliveryMetricsDto
{
    public long TotalAttempts { get; set; }
    public long SuccessfulDeliveries { get; set; }
    public long FailedDeliveries { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDeliveryTimeMs { get; set; }
    public Dictionary<string, long> StatusBreakdown { get; set; } = new();
}
