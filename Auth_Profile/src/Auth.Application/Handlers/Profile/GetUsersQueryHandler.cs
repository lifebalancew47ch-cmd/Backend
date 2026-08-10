using Auth.Application.DTOs.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Profile;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Profile;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, ApiResponse<PagedResult<UserProfileDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<GetUsersQueryHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<UserProfileDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // DevSecOps: clamp pagination (1..100) to prevent unbounded result sets / DoS via pageSize.
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var totalCount = await _userRepository.CountAsync(cancellationToken);
        var users = await _userRepository.GetAllAsync(page, pageSize, cancellationToken);
        
        var userDtos = _mapper.Map<IEnumerable<UserProfileDto>>(users).ToList();

        var pagedResult = new PagedResult<UserProfileDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
        return ApiResponse<PagedResult<UserProfileDto>>.SuccessResponse(pagedResult);
    }
}
