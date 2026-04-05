using AutoMapper;
using EventManager.Application.Services;
using EventManager.Models;

namespace EventManager.Tests.Fixtures;

/// <summary>
/// Фикстура для создания общего экземпляра EventService
/// </summary>
public class EventServiceFixture
{
    public EventService EventService { get; private set; }
    public IMapper Mapper { get; private set; }

    /// <summary>
    /// Конструктор, который настраивает AutoMapper и создает экземпляр EventService
    /// </summary>
    public EventServiceFixture()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EventSaveDto, EventEntity>();
            cfg.CreateMap<EventEntity, EventDto>();
            cfg.CreateMap<EventSaveDto, EventDto>();
        });
        
        Mapper = config.CreateMapper();
        EventService = new EventService(Mapper);
    }
}