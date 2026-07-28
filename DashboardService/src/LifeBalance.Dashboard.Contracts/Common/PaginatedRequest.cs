namespace LifeBalance.Dashboard.Contracts.Common;

/// <summary>
/// Standard paginated query parameters for list-based API requests.
/// </summary>
/// <param name="Page">The 1-based page number. Defaults to 1.</param>
/// <param name="PageSize">The number of items per page. Defaults to 20, max 100.</param>
public record PaginatedRequest(
    int Page = 1,
    int PageSize = 20);
