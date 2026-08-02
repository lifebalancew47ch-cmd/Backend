namespace LifeBalance.Administration.Domain.Enums;

/// <summary>Generic enabled/disabled status shared by managed entities.</summary>
public enum EntityStatus
{
    Active = 1,
    Inactive = 2
}

/// <summary>Data type of a system parameter value.</summary>
public enum ParameterDataType
{
    String = 1,
    Number = 2,
    Boolean = 3,
    Date = 4,
    Json = 5
}

/// <summary>Kind of operation recorded on an audit entry.</summary>
public enum AuditOperationType
{
    Create = 1,
    Read = 2,
    Update = 3,
    Delete = 4,
    Patch = 5,
    Login = 6,
    Logout = 7,
    Export = 8,
    Other = 9
}

/// <summary>Business area an audit entry belongs to.</summary>
public enum AuditEventType
{
    Authentication = 1,
    Configuration = 2,
    Parameter = 3,
    Catalog = 4,
    Audit = 5,
    Log = 6,
    Module = 7,
    Maintenance = 8,
    Service = 9,
    Security = 10,
    System = 11
}

/// <summary>Log severity levels (aligned with Microsoft.Extensions.Logging).</summary>
public enum SystemLogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

/// <summary>Well-known LifeBalance microservices monitored by the admin board.</summary>
public enum MicroserviceName
{
    Auth = 1,
    Organization = 2,
    Notifications = 3,
    MedicalData = 4,
    SedentaryEngine = 5,
    Dashboard = 6,
    Reporting = 7,
    MlPrediction = 8
}

/// <summary>Health classification returned by the service monitoring board.</summary>
public enum ServiceHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}
