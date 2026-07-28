using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditLogRepository auditLogRepository, ILogger<AuditService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task LogEventAsync(string? userId, AuthEventType eventType, string? details = null,
        string? ipAddress = null, string? userAgent = null, string? correlationId = null,
        string? resourceType = null, string? resourceId = null, bool success = true,
        string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = eventType.ToString(),
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            ResourceType = resourceType ?? "Auth",
            ResourceId = resourceId,
            Success = success,
            ErrorMessage = errorMessage
        };

        try
        {
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}", eventType);
        }
    }
}
