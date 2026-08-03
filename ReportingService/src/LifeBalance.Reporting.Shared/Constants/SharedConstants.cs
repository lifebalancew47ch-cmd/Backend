namespace LifeBalance.Reporting.Shared.Constants;

/// <summary>
/// Shared constants used across multiple layers and services.
/// </summary>
public static class SharedConstants
{
    /// <summary>HTTP header name for the Correlation ID.</summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>HTTP header name for the Request ID.</summary>
    public const string RequestIdHeader = "X-Request-ID";

    /// <summary>Claim type for the user identifier.</summary>
    public const string UserIdClaim = "sub";

    /// <summary>Claim type for the user email.</summary>
    public const string EmailClaim = "email";

    /// <summary>Claim type for user roles.</summary>
    public const string RolesClaim = "roles";

    /// <summary>Default page size for paginated queries.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Maximum allowed page size for paginated queries.</summary>
    public const int MaxPageSize = 100;
}
