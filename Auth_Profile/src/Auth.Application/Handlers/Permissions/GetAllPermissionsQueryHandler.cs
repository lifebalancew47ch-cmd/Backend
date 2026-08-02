using Auth.Application.DTOs.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Permissions;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Permissions;

public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, ApiResponse<IEnumerable<PermissionDto>>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllPermissionsQueryHandler> _logger;

    public GetAllPermissionsQueryHandler(
        IPermissionRepository permissionRepository,
        IMapper mapper,
        ILogger<GetAllPermissionsQueryHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<PermissionDto>>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var dtos = permissions.Select(permission => _mapper.Map<PermissionDto>(permission)).ToList();

        _logger.LogInformation("Retrieved {PermissionCount} permissions", dtos.Count);

        return ApiResponse<IEnumerable<PermissionDto>>.SuccessResponse(dtos);
    }
}
