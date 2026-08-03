using LifeBalance.Reporting.Shared.Constants;

namespace LifeBalance.Reporting.Contracts.Common;

/// <summary>
/// Common pagination parameters. Page index is zero based and the page size is
/// clamped to the range 1..<see cref="SharedConstants.MaxPageSize"/>.
/// </summary>
public class PaginatedRequest
{
    /// <summary>Gets or sets the zero based page index. Defaults to 0.</summary>
    public int PageIndex { get; set; }

    /// <summary>Gets or sets the page size. Defaults to <see cref="SharedConstants.DefaultPageSize"/>.</summary>
    public int PageSize { get; set; } = SharedConstants.DefaultPageSize;

    /// <summary>Normalizes the pagination values (clamps page size to 1..100).</summary>
    public void Normalize()
    {
        PageIndex = Math.Max(0, PageIndex);
        PageSize = Math.Clamp(PageSize, 1, SharedConstants.MaxPageSize);
    }
}
