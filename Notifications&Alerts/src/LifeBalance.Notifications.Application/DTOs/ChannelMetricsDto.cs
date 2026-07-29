namespace LifeBalance.Notifications.Application.DTOs;

public class ChannelMetricsDto
{
    public string Channel { get; set; } = string.Empty;
    public long Count { get; set; }
    public double Percentage { get; set; }
}
