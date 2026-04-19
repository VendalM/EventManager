using AutoMapper;
using EventManager.Models;

namespace EventManager.Mappers;

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
        CreateMap<EventEntity, EventDto>();
        
        CreateMap<EventSaveDto, EventEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, 
                opt => opt.MapFrom(src => src.StartDate!.Value))
            .ForMember(dest => dest.EndDate, 
                opt => opt.MapFrom(src => src.EndDate!.Value))
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src => src.TotalSeats));
        
        CreateMap<BookingDto, BookingEntity>()
            .ReverseMap();
    }
}
