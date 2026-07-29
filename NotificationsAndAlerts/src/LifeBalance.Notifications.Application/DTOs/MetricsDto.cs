namespace LifeBalance.Notifications.Application.DTOs;

public class MetricsDto
{
    public long TotalSent { get; set; }
    public long Delivered { get; set; }
    public long Failed { get; set; }
    public long Pending { get; set; }
    public long Opened { get; set; }
    public long Read { get; set; }
    public double Ctr { get; set; }
    public double AverageDeliveryTimeMs { get; set; }
    public string MostUsedChannel { get; set; } = string.Empty;
    public Dictionary<string, long> ChannelDistribution { get; set; } = new();
}
