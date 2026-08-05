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
        var totalCount = await _userRepository.CountAsync(cancellationToken);
        var users = await _userRepository.GetAllAsync(request.Page, request.PageSize, cancellationToken);
        
        var userDtos = _mapper.Map<IEnumerable<UserProfileDto>>(users).ToList();

        var pagedResult = new PagedResult<UserProfileDto>(userDtos, totalCount, request.Page, request.PageSize);
        return ApiResponse<PagedResult<UserProfileDto>>.SuccessResponse(pagedResult);
    }
}
