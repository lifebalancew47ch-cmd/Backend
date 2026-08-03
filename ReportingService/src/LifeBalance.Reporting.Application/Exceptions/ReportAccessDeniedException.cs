namespace LifeBalance.Reporting.Application.Exceptions;

/// <summary>
/// Exception raised when an authenticated user attempts to access a family or company
/// report they do not belong to (broken access control / IDOR prevention).
/// Converted to a 403 Forbidden response by the global exception middleware.
/// </summary>
public sealed class ReportAccessDeniedException : Exception
{
    /// <summary>Initializes a new instance of <see cref="ReportAccessDeniedException"/>.</summary>
    public ReportAccessDeniedException(string message)
        : base(message)
    {
    }
}
