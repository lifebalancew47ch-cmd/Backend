namespace LifeBalance.Reporting.Domain.Exceptions;

/// <summary>
/// Exception raised when a requested resource does not exist.
/// Mapped to HTTP 404 Not Found by the global exception middleware.
/// </summary>
public sealed class NotFoundException : DomainException
{
    /// <summary>Initializes a new instance of <see cref="NotFoundException"/>.</summary>
    public NotFoundException()
    {
    }

    /// <summary>Initializes a new instance of <see cref="NotFoundException"/> with a message.</summary>
    public NotFoundException(string message)
        : base(message)
    {
    }
}
