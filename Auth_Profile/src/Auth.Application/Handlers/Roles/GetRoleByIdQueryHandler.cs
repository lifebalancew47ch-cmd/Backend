using Auth.Application.DTOs.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Roles;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Roles;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, ApiResponse<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRoleByIdQueryHandler> _logger;

    public GetRoleByIdQueryHandler(
        IRoleRepository roleRepository,
        IMapper mapper,
        ILogger<GetRoleByIdQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);

        if (role is null)
            return ApiResponse<RoleDto>.FailResponse("Role not found.", statusCode: 404);

        _logger.LogInformation("Role retrieved: {RoleName}", role.Name);

        var dto = _mapper.Map<RoleDto>(role);
        return ApiResponse<RoleDto>.SuccessResponse(dto);
    }
}
