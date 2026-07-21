using AutoMapper;
using Contracts.Auth;
using Users.Application.Models;
using Users.Domain.Models;

namespace Users.Infrastructure.Mappers;

/// <summary>
/// Профиль AutoMapper для маппинга пользователей
/// </summary>
public class UserMapping : Profile
{
    /// <summary>
    /// Конструктор профиля, в котором настраиваются правила маппинга
    /// </summary>
    public UserMapping()
    {
        CreateMap<UserEntity, UserDto>();

        CreateMap<UserRegistrationDto, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore());
    }
}
