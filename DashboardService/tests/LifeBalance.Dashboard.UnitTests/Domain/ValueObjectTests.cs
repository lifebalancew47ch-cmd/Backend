using LifeBalance.Dashboard.Domain.Common;

namespace LifeBalance.Dashboard.UnitTests.Domain;

/// <summary>
/// Example unit test to verify the ValueObject equality contract.
/// Replace or extend with real domain ValueObject tests.
/// </summary>
public sealed class ValueObjectTests
{
    private sealed class TestValueObject(string value) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return value;
        }
    }

    [Fact]
    public void TwoValueObjectsWithSameData_ShouldBeEqual()
    {
        var a = new TestValueObject("hello");
        var b = new TestValueObject("hello");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void TwoValueObjectsWithDifferentData_ShouldNotBeEqual()
    {
        var a = new TestValueObject("hello");
        var b = new TestValueObject("world");

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }
}
