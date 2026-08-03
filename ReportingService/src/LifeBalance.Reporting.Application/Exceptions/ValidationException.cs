using FluentValidation.Results;

namespace LifeBalance.Reporting.Application.Exceptions;

/// <summary>
/// Exception raised when request validation fails.
/// Converted to a 422 Unprocessable Entity response by the global exception middleware.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Initializes a new instance of <see cref="ValidationException"/>.</summary>
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>Initializes a new instance of <see cref="ValidationException"/> from FluentValidation failures.</summary>
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the dictionary of property names and their validation error messages.</summary>
    public IDictionary<string, string[]> Errors { get; }
}
