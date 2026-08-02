using Auth.Application.DTOs.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Roles;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Roles;

public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, ApiResponse<IEnumerable<RoleDto>>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllRolesQueryHandler> _logger;

    public GetAllRolesQueryHandler(
        IRoleRepository roleRepository,
        IMapper mapper,
        ILogger<GetAllRolesQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<RoleDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var dtos = roles.Select(role => _mapper.Map<RoleDto>(role)).ToList();

        _logger.LogInformation("Retrieved {RoleCount} roles", dtos.Count);

        return ApiResponse<IEnumerable<RoleDto>>.SuccessResponse(dtos);
    }
}
