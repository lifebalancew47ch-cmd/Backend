using Auth.Application.Commands.Roles;
using Auth.Application.DTOs.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Roles;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, ApiResponse<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IAuditService auditService,
        IMapper mapper,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _roleRepository.ExistsByNameAsync(req.Name, cancellationToken: cancellationToken))
            return ApiResponse<RoleDto>.FailResponse("Role already exists.");

        var permissionIds = req.PermissionIds ?? new List<string>();

        if (permissionIds.Count > 0)
        {
            var permissions = await _permissionRepository.GetByIdsAsync(permissionIds, cancellationToken);

            if (permissions.Count() != permissionIds.Count)
                return ApiResponse<RoleDto>.FailResponse("One or more permissions do not exist.");
        }

        var role = new Role
        {
            Name = req.Name,
            NormalizedName = req.Name.ToUpperInvariant(),
            Description = req.Description,
            PermissionIds = permissionIds
        };

        await _roleRepository.AddAsync(role, cancellationToken);

        await _auditService.LogEventAsync(null, AuthEventType.RoleChange,
            $"Role created: {role.Name}", cancellationToken: cancellationToken);

        _logger.LogInformation("Role created: {RoleName}", role.Name);

        var dto = _mapper.Map<RoleDto>(role);
        return ApiResponse<RoleDto>.SuccessResponse(dto, statusCode: 201);
    }
}
