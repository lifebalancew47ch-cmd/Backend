using FluentValidation.Results;

namespace LifeBalance.Dashboard.Application.Exceptions;

/// <summary>
/// Exception raised when one or more FluentValidation validators fail in the pipeline.
/// Converted to a 422 Unprocessable Entity response by the global exception middleware.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Gets the validation errors grouped by property name.</summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> from a list of
    /// FluentValidation <see cref="ValidationFailure"/> instances.
    /// </summary>
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures have occurred.")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(
                failureGroup => failureGroup.Key,
                failureGroup => failureGroup.ToArray());
    }
}
