using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Infrastructure.Services;

/// <summary>
/// Persists audit entries in the <c>audit_logs</c> collection.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IRepository<AuditLog> _auditRepository;

    public AuditService(IRepository<AuditLog> auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task RecordAsync(AuditEntryDto entry, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            entry.UserId,
            entry.UserEmail,
            entry.Action,
            entry.EntityName,
            entry.EntityId,
            entry.OperationType,
            entry.EventType,
            entry.Service,
            entry.Endpoint,
            entry.IpAddress,
            entry.UserAgent,
            entry.CorrelationId,
            entry.RequestId,
            entry.Result,
            entry.DetailsJson,
            entry.OrganizationId,
            entry.CompanyId);

        await _auditRepository.AddAsync(auditLog, cancellationToken);
    }

    public async Task RecordAsync(IEnumerable<AuditEntryDto> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            await RecordAsync(entry, cancellationToken);
        }
    }
}
