namespace LifeBalance.Reporting.Contracts.Common;

/// <summary>
/// Standard paginated response envelope.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed class PaginatedResponse<T>
{
    /// <summary>Gets or sets the items for the current page.</summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>Gets or sets the total number of matching items across all pages.</summary>
    public int TotalItems { get; set; }

    /// <summary>Gets or sets the zero based page index.</summary>
    public int PageIndex { get; set; }

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    /// <summary>Gets a value indicating whether there are more items after this page.</summary>
    public bool HasNextPage => PageIndex < TotalPages - 1;

    /// <summary>Gets a value indicating whether there are items before this page.</summary>
    public bool HasPreviousPage => PageIndex > 0;
}
