namespace LifeBalance.Dashboard.Infrastructure.Options;

public class ServiceUrlsOptions
{
    public const string SectionName = "ServiceUrls";

    public string AuthServiceUrl { get; set; } = "http://localhost:5001";
    public string MedicalDataServiceUrl { get; set; } = "http://localhost:5002";
    public string SedentaryEngineServiceUrl { get; set; } = "http://localhost:5003";
    public string GamificationServiceUrl { get; set; } = "http://localhost:5004";
    public string NotificationServiceUrl { get; set; } = "http://localhost:5005";
    public string MlPredictionServiceUrl { get; set; } = "http://localhost:5006";
    public string OrganizationServiceUrl { get; set; } = "http://localhost:5007";
    public string ReportingServiceUrl { get; set; } = "http://localhost:5008";
}
