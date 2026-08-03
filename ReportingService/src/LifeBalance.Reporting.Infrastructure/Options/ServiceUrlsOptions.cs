namespace LifeBalance.Reporting.Infrastructure.Options;

/// <summary>
/// Configuration options for the upstream microservices consumed by the Reporting service.
/// Bound from <c>appsettings.json → ServiceUrls</c>.
/// </summary>
public sealed class ServiceUrlsOptions
{
    /// <summary>The configuration section key.</summary>
    public const string SectionName = "ServiceUrls";

    /// <summary>Gets or sets the Auth &amp; Profile service base URL.</summary>
    public string AuthServiceUrl { get; set; } = "http://localhost:5001";

    /// <summary>Gets or sets the Medical Data service base URL.</summary>
    public string MedicalDataServiceUrl { get; set; } = "http://localhost:5002";

    /// <summary>Gets or sets the Sedentary Engine service base URL.</summary>
    public string SedentaryEngineServiceUrl { get; set; } = "http://localhost:5003";

    /// <summary>Gets or sets the Dashboard service base URL.</summary>
    public string DashboardServiceUrl { get; set; } = "http://localhost:5004";

    /// <summary>Gets or sets the Organization &amp; SaaS service base URL.</summary>
    public string OrganizationServiceUrl { get; set; } = "http://localhost:5005";
}
