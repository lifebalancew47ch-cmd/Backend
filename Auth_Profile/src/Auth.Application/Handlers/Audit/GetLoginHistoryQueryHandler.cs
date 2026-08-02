using Auth.Application.DTOs.Audit;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Audit;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Audit;

public class GetLoginHistoryQueryHandler : IRequestHandler<GetLoginHistoryQuery, ApiResponse<PagedResult<LoginHistoryDto>>>
{
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetLoginHistoryQueryHandler> _logger;

    public GetLoginHistoryQueryHandler(
        ILoginHistoryRepository loginHistoryRepository,
        IMapper mapper,
        ILogger<GetLoginHistoryQueryHandler> logger)
    {
        _loginHistoryRepository = loginHistoryRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<LoginHistoryDto>>> Handle(GetLoginHistoryQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<LoginHistory> items = string.IsNullOrEmpty(request.UserId)
            ? await _loginHistoryRepository.GetAllAsync(request.Page, request.PageSize, cancellationToken)
            : await _loginHistoryRepository.GetByUserIdAsync(request.UserId, request.Page, request.PageSize, cancellationToken);

        var totalCount = await _loginHistoryRepository.CountAsync(cancellationToken);

        var dtoItems = items.Select(item => _mapper.Map<LoginHistoryDto>(item)).ToList();

        var paged = new PagedResult<LoginHistoryDto>
        {
            Items = dtoItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        _logger.LogInformation("Retrieved {HistoryCount} login history entries", dtoItems.Count);

        return ApiResponse<PagedResult<LoginHistoryDto>>.SuccessResponse(paged);
    }
}
