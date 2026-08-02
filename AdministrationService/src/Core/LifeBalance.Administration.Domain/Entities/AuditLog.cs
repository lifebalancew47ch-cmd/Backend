using LifeBalance.Administration.Domain.Common;
using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.Domain.Entities;

/// <summary>
/// Audit trail entry. Captures every administrative action performed against
/// the platform: who, what, when, from where and the final result.
/// </summary>
public class AuditLog : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public AuditOperationType OperationType { get; private set; } = AuditOperationType.Read;
    public AuditEventType EventType { get; private set; } = AuditEventType.System;
    public string Service { get; private set; } = "Administration";
    public string Endpoint { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string RequestId { get; private set; } = string.Empty;
    public bool Result { get; private set; } = true;
    public string? DetailsJson { get; private set; }
    public string? OrganizationId { get; private set; }
    public string? CompanyId { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private AuditLog() { }

    public AuditLog(string userId,
                    string userEmail,
                    string action,
                    string entityName,
                    string entityId,
                    AuditOperationType operationType,
                    AuditEventType eventType,
                    string service,
                    string endpoint,
                    string ipAddress,
                    string userAgent,
                    string correlationId,
                    string requestId,
                    bool result = true,
                    string? detailsJson = null,
                    string? organizationId = null,
                    string? companyId = null)
    {
        UserId = userId;
        UserEmail = userEmail;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        OperationType = operationType;
        EventType = eventType;
        Service = service;
        Endpoint = endpoint;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        RequestId = requestId;
        Result = result;
        DetailsJson = detailsJson;
        OrganizationId = organizationId;
        CompanyId = companyId;
        Timestamp = DateTime.UtcNow;
    }
}
