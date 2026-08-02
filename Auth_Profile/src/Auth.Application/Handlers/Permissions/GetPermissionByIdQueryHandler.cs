using Auth.Application.DTOs.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Queries.Permissions;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Permissions;

public class GetPermissionByIdQueryHandler : IRequestHandler<GetPermissionByIdQuery, ApiResponse<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPermissionByIdQueryHandler> _logger;

    public GetPermissionByIdQueryHandler(
        IPermissionRepository permissionRepository,
        IMapper mapper,
        ILogger<GetPermissionByIdQueryHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PermissionDto>> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (permission is null)
            return ApiResponse<PermissionDto>.FailResponse("Permission not found.", statusCode: 404);

        _logger.LogInformation("Permission retrieved: {PermissionName}", permission.Name);

        var dto = _mapper.Map<PermissionDto>(permission);
        return ApiResponse<PermissionDto>.SuccessResponse(dto);
    }
}
