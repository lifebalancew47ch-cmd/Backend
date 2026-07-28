namespace LifeBalance.Dashboard.Shared.Exceptions;

/// <summary>
/// Base exception for all LifeBalance application exceptions.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>Gets the HTTP status code associated with this exception.</summary>
    public int StatusCode { get; }

    /// <summary>Initializes a new instance of <see cref="AppException"/>.</summary>
    protected AppException(string message, int statusCode = 500)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
