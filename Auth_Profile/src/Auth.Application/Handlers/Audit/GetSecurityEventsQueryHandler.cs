using Auth.Application.DTOs.Audit;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Audit;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Audit;

public class GetSecurityEventsQueryHandler : IRequestHandler<GetSecurityEventsQuery, ApiResponse<PagedResult<AuditLogDto>>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSecurityEventsQueryHandler> _logger;

    public GetSecurityEventsQueryHandler(
        IAuditLogRepository auditLogRepository,
        IMapper mapper,
        ILogger<GetSecurityEventsQueryHandler> logger)
    {
        _auditLogRepository = auditLogRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<AuditLogDto>>> Handle(GetSecurityEventsQuery request, CancellationToken cancellationToken)
    {
        // DevSecOps: clamp pagination (1..100) to prevent unbounded result sets / DoS via pageSize.
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var logs = await _auditLogRepository.GetAllAsync(page, pageSize, cancellationToken);
        var totalCount = await _auditLogRepository.CountAsync(cancellationToken);

        var dtoItems = logs.Select(log => _mapper.Map<AuditLogDto>(log)).ToList();

        var paged = new PagedResult<AuditLogDto>
        {
            Items = dtoItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        _logger.LogInformation("Retrieved {LogCount} security events", dtoItems.Count);

        return ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(paged);
    }
}
