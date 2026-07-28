namespace LifeBalance.Dashboard.Domain.Common;

/// <summary>
/// Base class for Value Objects.
/// Value objects have no identity — equality is based on structural value comparison.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Returns the atomic components that contribute to value equality.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return ((ValueObject)obj).GetEqualityComponents()
            .SequenceEqual(GetEqualityComponents());
    }

    /// <inheritdoc/>
    public override int GetHashCode()
        => GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left?.Equals(right) ?? right is null;

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}
