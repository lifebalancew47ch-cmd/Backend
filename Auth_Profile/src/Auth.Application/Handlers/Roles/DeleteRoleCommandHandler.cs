using Auth.Application.Commands.Roles;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Enums;
using Auth.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Roles;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, ApiResponse<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<DeleteRoleCommandHandler> _logger;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        IAuditService auditService,
        ILogger<DeleteRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);

        if (role is null)
            return ApiResponse<bool>.FailResponse("Role not found.", statusCode: 404);

        await _roleRepository.DeleteAsync(request.Id, cancellationToken);

        await _auditService.LogEventAsync(null, AuthEventType.RoleChange,
            $"Role deleted: {role.Id}", cancellationToken: cancellationToken);

        _logger.LogInformation("Role deleted: {RoleId}", role.Id);

        return ApiResponse<bool>.SuccessResponse(true, "Role deleted successfully.");
    }
}
