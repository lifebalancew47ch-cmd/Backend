using Auth.Application.Commands.Permissions;
using Auth.Application.Interfaces.Repositories;
using Auth.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Permissions;

public class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand, ApiResponse<bool>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<DeletePermissionCommandHandler> _logger;

    public DeletePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        ILogger<DeletePermissionCommandHandler> logger)
    {
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (permission is null)
            return ApiResponse<bool>.FailResponse("Permission not found.", statusCode: 404);

        await _permissionRepository.DeleteAsync(request.Id, cancellationToken);

        _logger.LogInformation("Permission deleted: {PermissionId}", permission.Id);

        return ApiResponse<bool>.SuccessResponse(true, "Permission deleted successfully.");
    }
}
