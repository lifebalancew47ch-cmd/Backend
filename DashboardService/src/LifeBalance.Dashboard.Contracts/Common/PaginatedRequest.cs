namespace LifeBalance.Dashboard.Contracts.Common;

/// <summary>
/// Standard paginated query parameters for list-based API requests.
/// </summary>
/// <param name="Page">The 1-based page number. Defaults to 1.</param>
/// <param name="PageSize">The number of items per page. Defaults to 20, max 100.</param>
public record PaginatedRequest
{
    /// <summary>
    /// Initializes a new instance of <see cref="PaginatedRequest"/> with the page
    /// size clamped to the inclusive range [1, 100].
    /// </summary>
    public PaginatedRequest(int page = 1, int pageSize = 20)
    {
        Page = Math.Max(page, 1);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }

    /// <summary>Gets the 1-based page number.</summary>
    public int Page { get; }

    /// <summary>Gets the number of items per page (1-100).</summary>
    public int PageSize { get; }
}
