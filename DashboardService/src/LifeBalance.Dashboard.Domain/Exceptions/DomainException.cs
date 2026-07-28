namespace LifeBalance.Dashboard.Domain.Exceptions;

/// <summary>
/// Represents a domain rule violation that prevents a business operation from completing.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Initializes a new instance of <see cref="DomainException"/>.</summary>
    /// <param name="message">The message that describes the domain rule violation.</param>
    public DomainException(string message) : base(message) { }

    /// <summary>Initializes a new instance of <see cref="DomainException"/> with an inner exception.</summary>
    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
