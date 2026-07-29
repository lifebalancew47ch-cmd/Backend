using LifeBalance.OrganizationSaaS.Domain.Common;

namespace LifeBalance.OrganizationSaaS.Domain.ValueObjects;

public class PlanLimits : ValueObject
{
    public int MaxUsers { get; set; }
    public int MaxFamilies { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxDepartments { get; set; }
    public int MaxTeams { get; set; }
    public int MaxLicenses { get; set; }
    public int DataRetentionDays { get; set; }
    public bool DashboardsAvailable { get; set; }
    public bool ReportsAvailable { get; set; }
    public bool IaEnabled { get; set; }
    public bool GamificationEnabled { get; set; }
    public bool NotificationsEnabled { get; set; }
    public bool ApiAccess { get; set; }

    public static PlanLimits DefaultFree() => new()
    {
        MaxUsers = 5,
        MaxFamilies = 1,
        MaxCompanies = 1,
        MaxDepartments = 2,
        MaxTeams = 2,
        MaxLicenses = 5,
        DataRetentionDays = 30,
        DashboardsAvailable = true,
        ReportsAvailable = false,
        IaEnabled = false,
        GamificationEnabled = true,
        NotificationsEnabled = true,
        ApiAccess = false
    };

    public static PlanLimits DefaultEnterprise() => new()
    {
        MaxUsers = 10000,
        MaxFamilies = 500,
        MaxCompanies = 50,
        MaxDepartments = 200,
        MaxTeams = 1000,
        MaxLicenses = 10000,
        DataRetentionDays = 365,
        DashboardsAvailable = true,
        ReportsAvailable = true,
        IaEnabled = true,
        GamificationEnabled = true,
        NotificationsEnabled = true,
        ApiAccess = true
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxUsers;
        yield return MaxFamilies;
        yield return MaxCompanies;
        yield return MaxDepartments;
        yield return MaxTeams;
        yield return MaxLicenses;
        yield return DataRetentionDays;
        yield return DashboardsAvailable;
        yield return ReportsAvailable;
        yield return IaEnabled;
        yield return GamificationEnabled;
        yield return NotificationsEnabled;
        yield return ApiAccess;
    }
}
