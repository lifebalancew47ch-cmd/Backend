using LifeBalance.OrganizationSaaS.Domain.Common;

namespace LifeBalance.OrganizationSaaS.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}

public class ContactInfo : ValueObject
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Email;
        yield return Phone;
        yield return ContactPerson;
    }
}

public class WorkHours : ValueObject
{
    public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0);
    public List<DayOfWeek> WorkDays { get; set; } = new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartTime;
        yield return EndTime;
        foreach (var day in WorkDays)
        {
            yield return day;
        }
    }
}
