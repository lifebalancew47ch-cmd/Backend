namespace LifeBalance.Reporting.Infrastructure.Options;

/// <summary>
/// Configuration options for the outbound HTTP clients.
/// Bound from <c>appsettings.json → HttpClients</c>.
/// </summary>
public sealed class HttpClientOptions
{
    /// <summary>The configuration section key.</summary>
    public const string SectionName = "HttpClients";

    /// <summary>Gets or sets the request timeout in seconds. Defaults to 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Gets or sets the number of retry attempts. Defaults to 3.</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Gets or sets the number of consecutive failures before the circuit opens. Defaults to 5.</summary>
    public int CircuitBreakerFailures { get; set; } = 5;

    /// <summary>Gets or sets the circuit breaker cooldown in seconds. Defaults to 30.</summary>
    public int CircuitBreakerCooldownSeconds { get; set; } = 30;

    /// <summary>Gets or sets the health probe timeout in seconds. Defaults to 3.</summary>
    public int HealthProbeTimeoutSeconds { get; set; } = 3;
}
