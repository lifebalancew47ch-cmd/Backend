using Auth.Application.Commands.Permissions;
using Auth.Application.DTOs.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Entities;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Permissions;

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, ApiResponse<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreatePermissionCommandHandler> _logger;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IMapper mapper,
        ILogger<CreatePermissionCommandHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PermissionDto>> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _permissionRepository.ExistsByNameAsync(req.Name, cancellationToken: cancellationToken))
            return ApiResponse<PermissionDto>.FailResponse("Permission already exists.");

        var permission = new Permission
        {
            Name = req.Name,
            NormalizedName = req.Name.ToUpperInvariant(),
            Description = req.Description,
            Module = req.Module
        };

        await _permissionRepository.AddAsync(permission, cancellationToken);

        _logger.LogInformation("Permission created: {PermissionName}", permission.Name);

        var dto = _mapper.Map<PermissionDto>(permission);
        return ApiResponse<PermissionDto>.SuccessResponse(dto, statusCode: 201);
    }
}
