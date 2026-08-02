using System;

namespace LifeBalance.Notifications.Shared.Exceptions;

public class ApiException : Exception
{
    public ApiException() : base() {}

    public ApiException(string message) : base(message) {}

    public ApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public ApiException(string message, Exception innerException) : base(message, innerException) {}

    public int StatusCode { get; set; } = 500;
}
