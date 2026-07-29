namespace LifeBalance.Notifications.Application.Interfaces;

public class PredictAlertRequest
{
    public string UserId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public int SedentaryRisk { get; set; }
}

public interface IMLServiceClient
{
    Task ProcessPredictiveAlertAsync(PredictAlertRequest request);
    Task ProcessRecommendationAsync(PredictAlertRequest request);
    Task ProcessSedentaryRiskAsync(PredictAlertRequest request);
}
