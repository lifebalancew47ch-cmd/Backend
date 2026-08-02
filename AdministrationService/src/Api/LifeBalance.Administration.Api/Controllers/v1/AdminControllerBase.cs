using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeBalance.Administration.Application.Common.Constants;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[Produces("application/json")]
[Authorize(Policy = AdministrationRoles.AdministratorOnlyPolicy)]
public abstract class AdminControllerBase : ControllerBase
{
    protected IMediator Mediator { get; }
    protected ICurrentUser CurrentUser { get; }
    protected IAuditService Audit { get; }

    protected AdminControllerBase(IMediator mediator, ICurrentUser currentUser, IAuditService audit)
    {
        Mediator = mediator;
        CurrentUser = currentUser;
        Audit = audit;
    }

    /// <summary>
    /// Writes an audit trail entry for the current administrative operation.
    /// The identity is ALWAYS taken from the JWT claims (anti-IDOR).
    /// </summary>
    protected async Task RecordAuditAsync(
        string action,
        string entityName,
        string entityId,
        AuditOperationType operationType,
        AuditEventType eventType,
        bool result = true,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntryDto(
            UserId: CurrentUser.UserId ?? "system",
            UserEmail: CurrentUser.UserEmail ?? string.Empty,
            Action: action,
            EntityName: entityName,
            EntityId: entityId,
            OperationType: operationType,
            EventType: eventType,
            Service: "AdministrationService",
            Endpoint: $"{Request.Method} {Request.Path}",
            IpAddress: CurrentUser.IpAddress ?? string.Empty,
            UserAgent: CurrentUser.UserAgent ?? string.Empty,
            CorrelationId: CurrentUser.CorrelationId ?? string.Empty,
            RequestId: CurrentUser.RequestId ?? string.Empty,
            Result: result,
            DetailsJson: details);

        await Audit.RecordAsync(entry, cancellationToken);
    }
}
