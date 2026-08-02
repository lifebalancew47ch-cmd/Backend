using Auth.Application.Commands.Profile;
using Auth.Application.DTOs.Profile;
using Auth.Application.Interfaces.Repositories;
using Auth.Application.Interfaces.Services;
using Auth.Domain.Enums;
using Auth.Shared.Common;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Handlers.Profile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ApiResponse<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        IAuditService auditService,
        IMapper mapper,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return ApiResponse<UserProfileDto>.FailResponse("User not found.", statusCode: 404);

        user.FirstName = request.Request.FirstName;
        user.LastName = request.Request.LastName;

        if (request.Request.PhoneNumber is not null)
            user.PhoneNumber = request.Request.PhoneNumber;

        if (request.Request.AvatarUrl is not null)
            user.AvatarUrl = request.Request.AvatarUrl;

        user.MarkUpdated();
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _auditService.LogEventAsync(user.Id, AuthEventType.ProfileUpdate,
            "Profile updated", cancellationToken: cancellationToken);

        _logger.LogInformation("Profile updated for user {UserId}", user.Id);

        var profile = _mapper.Map<UserProfileDto>(user);
        return ApiResponse<UserProfileDto>.SuccessResponse(profile);
    }
}
