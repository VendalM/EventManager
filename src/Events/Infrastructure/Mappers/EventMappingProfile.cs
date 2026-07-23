using AutoMapper;
using Contracts.Events;
using Events.Application.Models;
using Events.Domain.Models;

namespace Events.Infrastructure.Mappers;

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
    }
}
