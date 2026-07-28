namespace LifeBalance.Dashboard.Domain.ValueObjects;

public record KpiMetric(string Name, double Value, string Unit, double PercentageChange, string Status);

public record HealthScore(double OverallScore, string Category, DateTime EvaluatedAtUtc);

public record BiometricSummary(double HeartRateBpm, double BloodPressureSystolic, double BloodPressureDiastolic, double WeightKg, double Bmi);

public record GoalProgressInfo(string GoalId, string Title, double TargetValue, double CurrentValue, double ProgressPercentage, bool IsCompleted);

public record ServiceCallMetrics(string ServiceName, string Endpoint, int StatusCode, double DurationMs, bool Success);
