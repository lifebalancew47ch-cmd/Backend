using AutoMapper;
using Auth.Application.DTOs.Permissions;
using Auth.Domain.Entities;

namespace Auth.Application.Mappings;

public class PermissionMappingProfile : Profile
{
    public PermissionMappingProfile()
    {
        CreateMap<Permission, PermissionDto>()
            .ConstructUsing(src => new PermissionDto(
                src.Id,
                src.Name,
                src.Description,
                src.Module,
                src.CreatedAt));
    }
}
