using Application.Models;
using AutoMapper;
using Domain.Models;

namespace Infrastructure.Mappers;

/// <summary>
/// Профиль AutoMapper для маппинга событий
/// </summary>
public class EventMappingProfile : Profile
{
    /// <summary>
    /// Конструктор профиля, в котором настраиваются правила маппинга
    /// </summary>
    public EventMappingProfile()
    {
        CreateMap<EventEntity, EventDto>()
            .ReverseMap();

        CreateMap<EventSaveDto, EventEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate,
                opt => opt.MapFrom(src => src.StartDate!.Value))
            .ForMember(dest => dest.EndDate,
                opt => opt.MapFrom(src => src.EndDate!.Value));
        
        CreateMap<BookingDto, BookingEntity>()
            .ReverseMap();

        CreateMap<UserEntity, UserDto>();

        CreateMap<UserRegistrationDto, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore());
    }
}
