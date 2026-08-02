using Auth.Application.Commands.Roles;
using Auth.Application.DTOs.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Enums;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Roles;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, ApiResponse<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IAuditService auditService,
        IMapper mapper,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);

        if (role is null)
            return ApiResponse<RoleDto>.FailResponse("Role not found.", statusCode: 404);

        if (await _roleRepository.ExistsByNameAsync(req.Name, request.Id, cancellationToken))
            return ApiResponse<RoleDto>.FailResponse("Role already exists.");

        var permissionIds = req.PermissionIds ?? new List<string>();

        if (permissionIds.Count > 0)
        {
            var permissions = await _permissionRepository.GetByIdsAsync(permissionIds, cancellationToken);

            if (permissions.Count() != permissionIds.Count)
                return ApiResponse<RoleDto>.FailResponse("One or more permissions do not exist.");
        }

        role.Name = req.Name;
        role.NormalizedName = req.Name.ToUpperInvariant();
        role.Description = req.Description;
        role.PermissionIds = permissionIds;
        role.MarkUpdated();
        await _roleRepository.UpdateAsync(role, cancellationToken);

        await _auditService.LogEventAsync(null, AuthEventType.RoleChange,
            $"Role updated: {role.Name}", cancellationToken: cancellationToken);

        _logger.LogInformation("Role updated: {RoleName}", role.Name);

        var dto = _mapper.Map<RoleDto>(role);
        return ApiResponse<RoleDto>.SuccessResponse(dto);
    }
}
