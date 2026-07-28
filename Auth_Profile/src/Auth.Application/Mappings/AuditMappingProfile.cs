using AutoMapper;
using Auth.Application.DTOs.Audit;
using Auth.Domain.Entities;

namespace Auth.Application.Mappings;

public class AuditMappingProfile : Profile
{
    public AuditMappingProfile()
    {
        CreateMap<LoginHistory, LoginHistoryDto>()
            .ConstructUsing(src => new LoginHistoryDto(
                src.Id,
                src.Email,
                src.IpAddress,
                src.UserAgent,
                src.Device,
                src.Success,
                src.FailureReason,
                src.LoginAt));

        CreateMap<AuditLog, AuditLogDto>()
            .ConstructUsing(src => new AuditLogDto(
                src.Id,
                src.UserId,
                src.Action,
                src.Details,
                src.IpAddress,
                src.ResourceType,
                src.Success,
                src.ErrorMessage,
                src.CreatedAt));
    }
}
