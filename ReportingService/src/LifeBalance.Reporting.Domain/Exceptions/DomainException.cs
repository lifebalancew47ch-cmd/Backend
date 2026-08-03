namespace LifeBalance.Reporting.Domain.Exceptions;

/// <summary>
/// Base exception for all domain rule violations.
/// Mapped to HTTP 400 Bad Request by the global exception middleware.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Initializes a new instance of <see cref="DomainException"/>.</summary>
    public DomainException()
    {
    }

    /// <summary>Initializes a new instance of <see cref="DomainException"/> with a message.</summary>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="DomainException"/> with a message and inner exception.</summary>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
