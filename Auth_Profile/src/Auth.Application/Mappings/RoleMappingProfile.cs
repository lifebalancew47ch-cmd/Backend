using AutoMapper;
using Auth.Application.DTOs.Roles;
using Auth.Domain.Entities;

namespace Auth.Application.Mappings;

public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Role, RoleDto>()
            .ConstructUsing(src => new RoleDto(
                src.Id,
                src.Name,
                src.Description,
                src.PermissionIds,
                src.CreatedAt));
    }
}
