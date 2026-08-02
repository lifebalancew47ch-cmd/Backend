using LifeBalance.Administration.Domain.Common;
using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Last known health snapshot of a LifeBalance microservice. Persisted by the
/// monitoring aggregator so the status board survives restarts.
/// </summary>
public class ServiceStatus : AggregateRoot
{
    public MicroserviceName Service { get; private set; } = MicroserviceName.Auth;
    public string ServiceName { get; private set; } = string.Empty;
    public ServiceHealthStatus Status { get; private set; } = ServiceHealthStatus.Unknown;
    public int? StatusCode { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public long LatencyMs { get; private set; }
    public string? ServiceVersion { get; private set; }
    public string? PayloadJson { get; private set; }
    public DateTime LastCheckedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastSuccessAt { get; private set; }

    private ServiceStatus() { }

    public ServiceStatus(MicroserviceName service, string serviceName)
    {
        Service = service;
        ServiceName = serviceName;
    }

    public void Report(ServiceHealthStatus status,
                       int? statusCode,
                       string message,
                       long latencyMs,
                       string? serviceVersion,
                       string? payloadJson,
                       DateTime checkedAt)
    {
        Status = status;
        StatusCode = statusCode;
        Message = message;
        LatencyMs = latencyMs;
        ServiceVersion = serviceVersion;
        PayloadJson = payloadJson;
        LastCheckedAt = checkedAt;
        if (status == ServiceHealthStatus.Healthy)
        {
            LastSuccessAt = checkedAt;
        }
    }
}
