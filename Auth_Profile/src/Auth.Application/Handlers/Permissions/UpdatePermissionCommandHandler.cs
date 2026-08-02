using Auth.Application.Commands.Permissions;
using Auth.Application.DTOs.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Permissions;

public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, ApiResponse<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdatePermissionCommandHandler> _logger;

    public UpdatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IMapper mapper,
        ILogger<UpdatePermissionCommandHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PermissionDto>> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (permission is null)
            return ApiResponse<PermissionDto>.FailResponse("Permission not found.", statusCode: 404);

        if (await _permissionRepository.ExistsByNameAsync(req.Name, request.Id, cancellationToken))
            return ApiResponse<PermissionDto>.FailResponse("Permission already exists.");

        permission.Name = req.Name;
        permission.NormalizedName = req.Name.ToUpperInvariant();
        permission.Description = req.Description;
        permission.Module = req.Module;
        permission.MarkUpdated();
        await _permissionRepository.UpdateAsync(permission, cancellationToken);

        _logger.LogInformation("Permission updated: {PermissionName}", permission.Name);

        var dto = _mapper.Map<PermissionDto>(permission);
        return ApiResponse<PermissionDto>.SuccessResponse(dto);
    }
}
