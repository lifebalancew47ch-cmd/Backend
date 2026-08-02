namespace LifeBalance.Dashboard.Application.Common.Interfaces;

public record MedicalDataResponseDto(string UserId, double HeartRate, double SystolicBp, double DiastolicBp, double Weight, double Height, double Bmi, DateTime RecordedAt);

public interface IMedicalDataServiceClient
{
    Task<MedicalDataResponseDto?> GetUserBiometricsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<MedicalDataResponseDto>?> GetFamilyBiometricsAsync(string familyId, CancellationToken cancellationToken = default);
}
