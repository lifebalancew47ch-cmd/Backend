namespace LifeBalance.Dashboard.Domain.Exceptions;

/// <summary>
/// Raised when a required entity cannot be found by its identifier.
/// </summary>
public sealed class NotFoundException : DomainException
{
    /// <summary>Initializes a new instance of <see cref="NotFoundException"/>.</summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="id">The identifier that was searched.</param>
    public NotFoundException(string entityName, object id)
        : base($"Entity '{entityName}' with Id '{id}' was not found.") { }
}
