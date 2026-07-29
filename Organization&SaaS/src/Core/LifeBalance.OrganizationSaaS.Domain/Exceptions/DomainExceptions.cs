namespace LifeBalance.OrganizationSaaS.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class MultiTenantViolationException : DomainException
{
    public MultiTenantViolationException(string message = "Cross-tenant access attempt detected and blocked.")
        : base(message) { }
}

public class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string resourceName, object key)
        : base($"Resource '{resourceName}' with key '{key}' was not found.") { }
}

public class LimitExceededException : DomainException
{
    public LimitExceededException(string resourceName, int maxLimit)
        : base($"SaaS Plan limit reached for '{resourceName}'. Maximum allowed is {maxLimit}.") { }
}
