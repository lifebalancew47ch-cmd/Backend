using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Contracts.Common;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.Repositories;
using LifeBalance.Reporting.Shared.Results;

namespace LifeBalance.Reporting.Application.Features.ReportHistory;

public sealed record GetReportHistoryQuery(
    string UserId,
    int PageIndex,
    int PageSize,
    ReportScope? Scope,
    ReportFormat? Format) : IRequest<Result<PaginatedResponse<ReportHistoryItemDto>>>;

public sealed record ReportHistoryItemDto(
    string Id,
    ReportScope Scope,
    string? ScopeId,
    ReportFormat? Format,
    ReportStatus Status,
    double DurationMs,
    int RecordCount,
    DateTime TimestampUtc);

/// <summary>
/// Returns the paginated report generation history of the requesting user.
/// </summary>
public sealed class GetReportHistoryQueryHandler : IRequestHandler<GetReportHistoryQuery, Result<PaginatedResponse<ReportHistoryItemDto>>>
{
    private readonly IReportGenerationLogRepository _repository;
    private readonly IMapper _mapper;

    public GetReportHistoryQueryHandler(
        IReportGenerationLogRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResponse<ReportHistoryItemDto>>> Handle(
        GetReportHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? Shared.Constants.SharedConstants.DefaultPageSize : request.PageSize, 1, Shared.Constants.SharedConstants.MaxPageSize);

        var (items, total) = await _repository.GetByUserAsync(
            request.UserId,
            pageIndex,
            pageSize,
            request.Scope,
            request.Format,
            cancellationToken);

        return Result.Success(new PaginatedResponse<ReportHistoryItemDto>
        {
            Items = _mapper.Map<IReadOnlyList<ReportHistoryItemDto>>(items),
            TotalItems = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        });
    }
}
