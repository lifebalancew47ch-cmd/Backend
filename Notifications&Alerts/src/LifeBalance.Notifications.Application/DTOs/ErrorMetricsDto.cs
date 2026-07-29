namespace LifeBalance.Notifications.Application.DTOs;

public class ErrorMetricsDto
{
    public long TotalErrors { get; set; }
    public List<ErrorDetail> RecentErrors { get; set; } = new();
    public Dictionary<string, long> ErrorByType { get; set; } = new();
}

public class ErrorDetail
{
    public string NotificationId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
