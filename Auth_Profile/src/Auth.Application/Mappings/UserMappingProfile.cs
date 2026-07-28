using AutoMapper;
using Auth.Application.DTOs.Profile;
using Auth.Domain.Entities;

namespace Auth.Application.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserProfileDto>()
            .ConstructUsing(src => new UserProfileDto(
                src.Id,
                src.Email,
                src.Username,
                src.FirstName,
                src.LastName,
                src.PhoneNumber,
                src.AvatarUrl,
                src.IsEmailConfirmed,
                src.IsActive,
                src.CreatedAt,
                src.LastLoginAt));
    }
}
