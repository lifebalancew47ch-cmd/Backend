using MediatR;
using LifeBalance.Administration.Application.Common.Mappings;
using LifeBalance.Administration.Application.Common.Models;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;
using LifeBalance.Administration.Domain.Exceptions;
using LifeBalance.Administration.Domain.Interfaces;

namespace LifeBalance.Administration.Application.Features.Audit;

public record AuditLogDto(
    string Id,
    string UserId,
    string UserEmail,
    string Action,
    string EntityName,
    string EntityId,
    string OperationType,
    string EventType,
    string Service,
    string Endpoint,
    string IpAddress,
    string UserAgent,
    string CorrelationId,
    string RequestId,
    bool Result,
    string? DetailsJson,
    string? OrganizationId,
    string? CompanyId,
    DateTime Timestamp);

// ── Queries ───────────────────────────────────────────────────────────────
public record GetAuditLogByIdQuery(string Id) : IRequest<ApiResponse<AuditLogDto>>;

public record GetAuditLogsPagedQuery(
    int PageIndex = 1,
    int PageSize = 10,
    string? UserId = null,
    string? Service = null,
    AuditEventType? EventType = null,
    string? OrganizationId = null,
    string? CompanyId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<ApiResponse<PagedResult<AuditLogDto>>>;

public record GetAuditLogsByUserQuery(string UserId, int PageIndex = 1, int PageSize = 10)
    : IRequest<ApiResponse<PagedResult<AuditLogDto>>>;

public record GetAuditLogsByServiceQuery(string Service, int PageIndex = 1, int PageSize = 10)
    : IRequest<ApiResponse<PagedResult<AuditLogDto>>>;

// ── Query Handler ─────────────────────────────────────────────────────────
public class AuditQueryHandler :
    IRequestHandler<GetAuditLogByIdQuery, ApiResponse<AuditLogDto>>,
    IRequestHandler<GetAuditLogsPagedQuery, ApiResponse<PagedResult<AuditLogDto>>>,
    IRequestHandler<GetAuditLogsByUserQuery, ApiResponse<PagedResult<AuditLogDto>>>,
    IRequestHandler<GetAuditLogsByServiceQuery, ApiResponse<PagedResult<AuditLogDto>>>
{
    private readonly IRepository<AuditLog> _auditRepository;

    public AuditQueryHandler(IRepository<AuditLog> auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<ApiResponse<AuditLogDto>> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
    {
        var log = await _auditRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(AuditLog), request.Id);

        return ApiResponse<AuditLogDto>.Ok(AdministrationMappings.ToDto(log));
    }

    public async Task<ApiResponse<PagedResult<AuditLogDto>>> Handle(GetAuditLogsPagedQuery request, CancellationToken cancellationToken)
    {
        var user = Normalize(request.UserId);
        var service = Normalize(request.Service);
        var org = Normalize(request.OrganizationId);
        var company = Normalize(request.CompanyId);

        var (items, total) = await _auditRepository.GetPagedAsync(
            x => (string.IsNullOrEmpty(user) || x.UserId == user)
                 && (string.IsNullOrEmpty(service) || x.Service == service)
                 && (request.EventType == null || x.EventType == request.EventType.Value)
                 && (string.IsNullOrEmpty(org) || x.OrganizationId == org)
                 && (string.IsNullOrEmpty(company) || x.CompanyId == company)
                 && (request.FromDate == null || x.Timestamp >= request.FromDate.Value)
                 && (request.ToDate == null || x.Timestamp <= request.ToDate.Value),
            request.PageIndex,
            request.PageSize,
            x => x.Timestamp,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>(dtos, request.PageIndex, request.PageSize, total));
    }

    public async Task<ApiResponse<PagedResult<AuditLogDto>>> Handle(GetAuditLogsByUserQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _auditRepository.GetPagedAsync(
            x => x.UserId == request.UserId,
            request.PageIndex,
            request.PageSize,
            x => x.Timestamp,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>(dtos, request.PageIndex, request.PageSize, total));
    }

    public async Task<ApiResponse<PagedResult<AuditLogDto>>> Handle(GetAuditLogsByServiceQuery request, CancellationToken cancellationToken)
    {
        var service = Normalize(request.Service);
        var (items, total) = await _auditRepository.GetPagedAsync(
            x => x.Service == service,
            request.PageIndex,
            request.PageSize,
            x => x.Timestamp,
            sortDescending: true,
            cancellationToken);

        var dtos = items.Select(AdministrationMappings.ToDto);
        return ApiResponse<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>(dtos, request.PageIndex, request.PageSize, total));
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
