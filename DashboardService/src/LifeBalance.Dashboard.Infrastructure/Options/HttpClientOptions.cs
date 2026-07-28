namespace LifeBalance.Dashboard.Infrastructure.Options;

/// <summary>
/// Configuration options for external HTTP service clients.
/// Bound from <c>appsettings.json → HttpClients</c>.
/// </summary>
public sealed class HttpClientOptions
{
    /// <summary>The configuration section key.</summary>
    public const string SectionName = "HttpClients";

    /// <summary>Gets or sets the base URL for the Habits microservice.</summary>
    public string HabitsServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the base URL for the Nutrition microservice.</summary>
    public string NutritionServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the base URL for the Fitness microservice.</summary>
    public string FitnessServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the base URL for the Sleep microservice.</summary>
    public string SleepServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the request timeout in seconds. Defaults to 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Gets or sets the number of retry attempts. Defaults to 3.</summary>
    public int RetryCount { get; set; } = 3;
}
