namespace LifeBalance.Reporting.Shared.Exceptions;

/// <summary>
/// Base exception for expected application-level errors.
/// </summary>
public class AppException : Exception
{
    /// <summary>Initializes a new instance of <see cref="AppException"/>.</summary>
    public AppException()
    {
    }

    /// <summary>Initializes a new instance of <see cref="AppException"/> with a message.</summary>
    public AppException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="AppException"/> with a message and inner exception.</summary>
    public AppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
