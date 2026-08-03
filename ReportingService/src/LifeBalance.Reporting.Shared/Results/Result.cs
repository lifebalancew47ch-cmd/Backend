namespace LifeBalance.Reporting.Shared.Results;

/// <summary>
/// Represents the outcome of an operation that may succeed or fail, without returning a value.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error message when the operation fails. Null on success.</summary>
    public string? Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true);

    /// <summary>Creates a failed result with the specified error message.</summary>
    public static Result Failure(string error) => new(false, error);

    /// <summary>Creates a successful result carrying a value.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>Creates a failed result carrying no value.</summary>
    public static Result<TValue> Failure<TValue>(string error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that may succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="TValue">The type of the returned value on success.</typeparam>
public sealed class Result<TValue>
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, string? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value. Only valid when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value on a failed result.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    /// <summary>Gets the error message when the operation fails. Null on success.</summary>
    public string? Error { get; }

    internal static Result<TValue> Success(TValue value) => new(true, value, null);
    internal static Result<TValue> Failure(string error) => new(false, default, error);
}
