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
        var logs = await _auditLogRepository.GetAllAsync(request.Page, request.PageSize, cancellationToken);
        var totalCount = await _auditLogRepository.CountAsync(cancellationToken);

        var dtoItems = logs.Select(log => _mapper.Map<AuditLogDto>(log)).ToList();

        var paged = new PagedResult<AuditLogDto>
        {
            Items = dtoItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        _logger.LogInformation("Retrieved {LogCount} security events", dtoItems.Count);

        return ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(paged);
    }
}
