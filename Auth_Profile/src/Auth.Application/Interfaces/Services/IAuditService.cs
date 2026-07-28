using Auth.Domain.Enums;

namespace Auth.Application.Interfaces.Services;

public interface IAuditService
{
    Task LogEventAsync(string? userId, AuthEventType eventType, string? details = null,
        string? ipAddress = null, string? userAgent = null, string? correlationId = null,
        string? resourceType = null, string? resourceId = null, bool success = true,
        string? errorMessage = null, CancellationToken cancellationToken = default);
}
