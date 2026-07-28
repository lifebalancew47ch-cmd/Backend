namespace LifeBalance.Dashboard.Domain.Specifications;

/// <summary>
/// Abstract base class for the Specification pattern.
/// Encapsulates a business rule as a reusable predicate.
/// </summary>
/// <typeparam name="T">The type of the object being evaluated.</typeparam>
public abstract class Specification<T>
{
    /// <summary>Evaluates whether the specification is satisfied by <paramref name="candidate"/>.</summary>
    /// <param name="candidate">The object to evaluate.</param>
    /// <returns><c>true</c> if satisfied; otherwise <c>false</c>.</returns>
    public abstract bool IsSatisfiedBy(T candidate);

    /// <summary>Combines two specifications with a logical AND.</summary>
    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);

    /// <summary>Combines two specifications with a logical OR.</summary>
    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);

    /// <summary>Negates this specification.</summary>
    public Specification<T> Not() => new NotSpecification<T>(this);
}

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate)
        => left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
}

internal sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate)
        => left.IsSatisfiedBy(candidate) || right.IsSatisfiedBy(candidate);
}

internal sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate)
        => !inner.IsSatisfiedBy(candidate);
}
