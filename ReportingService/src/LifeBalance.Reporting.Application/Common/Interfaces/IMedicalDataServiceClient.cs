namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// A single biometric reading as modeled by the Medical Data service.
/// All sensor values are nullable because not every reading populates every channel.
/// </summary>
public sealed record MedicalReadingDto(
    string Id,
    string UserId,
    string? FamilyId,
    string? CompanyId,
    double? HeartRate,
    double? Hrv,
    double? Spo2,
    int Steps,
    double? Latitude,
    double? Longitude,
    double? AccelerometerX,
    double? AccelerometerY,
    double? AccelerometerZ,
    double? GyroscopeX,
    double? GyroscopeY,
    double? GyroscopeZ,
    double? SystolicBp,
    double? DiastolicBp,
    double? Weight,
    double? Height,
    string? DeviceId,
    DateTime RecordedAtUtc,
    DateTime CreatedAtUtc);

/// <summary>
/// Latest biometric snapshot for a user.
/// </summary>
public sealed record LatestBiometricsDto(
    string UserId,
    double HeartRate,
    double SystolicBp,
    double DiastolicBp,
    double Weight,
    double Height,
    double Bmi,
    DateTime RecordedAtUtc);

/// <summary>
/// Daily platform activity count.
/// </summary>
public sealed record DailyActiveUsersDto(int ActiveUsersToday, DateTime AsOfUtc);

/// <summary>
/// Contract for the Medical Data microservice client.
/// All methods return <c>null</c> when the upstream call fails (fail-closed callers).
/// </summary>
public interface IMedicalDataServiceClient
{
    /// <summary>Retrieves the historical readings of a user within a date range.</summary>
    Task<IReadOnlyList<MedicalReadingDto>?> GetUserReadingsAsync(
        string userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the historical readings of all members of a family within a date range.</summary>
    Task<IReadOnlyList<MedicalReadingDto>?> GetFamilyReadingsAsync(
        string familyId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the historical readings of all employees of a company within a date range.</summary>
    Task<IReadOnlyList<MedicalReadingDto>?> GetCompanyReadingsAsync(
        string companyId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the latest biometric snapshot of a user.</summary>
    Task<LatestBiometricsDto?> GetLatestBiometricsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the number of users with readings in the last 24 hours.</summary>
    Task<DailyActiveUsersDto?> GetDailyActiveUsersAsync(CancellationToken cancellationToken = default);
}
