namespace LifeBalance.Administration.Domain.Exceptions;

/// <summary>Base type for all domain-level exceptions.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Thrown when a requested resource cannot be found.</summary>
public class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string resourceName, object key)
        : base($"Resource '{resourceName}' with key '{key}' was not found.") { }
}

/// <summary>Thrown when an operation conflicts with the current state of the system.</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Thrown when the caller is not allowed to perform an operation.</summary>
public class UnauthorizedOperationException : DomainException
{
    public UnauthorizedOperationException(string message = "You are not authorized to perform this operation.")
        : base(message) { }
}

/// <summary>Thrown when an upstream microservice is unavailable (fail-closed policy).</summary>
public class UpstreamServiceUnavailableException : DomainException
{
    public UpstreamServiceUnavailableException(string service)
        : base($"Upstream service '{service}' is unavailable. Please try again later.") { }
}

/// <summary>Thrown while the platform is in maintenance mode.</summary>
public class MaintenanceModeException : DomainException
{
    public MaintenanceModeException(string message = "The platform is currently under maintenance. Please try again later.")
        : base(message) { }
}

/// <summary>Thrown when an entity value violates an invariant (e.g. bad e-mail).</summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message) { }
}
