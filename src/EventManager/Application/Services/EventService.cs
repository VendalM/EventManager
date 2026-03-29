using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Models;

namespace EventManager.Application.Services;

/// <summary>
/// Сервис для обработки событий
/// </summary>
public class EventService : IEventService
{
    /// <summary>
    /// Все доступные события (временное хранилище)
    /// </summary>
    private static List<EventEntity> Events { get; set; } = new List<EventEntity>();
    
    private readonly IMapper _mapper;

    /// <summary>
    /// Конструктор сервиса событий
    /// </summary>
    /// <param name="mapper">AutoMapper для преобразования сущностей</param>
    public EventService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    /// <inheritdoc />
    public List<EventDto> GetAllEvents(string? title, DateTime? from, DateTime? to)
    {
        var query = Events.AsQueryable();
        
        if (!string.IsNullOrEmpty(title))
        {
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }
    
        if (from.HasValue)
        {
            query = query.Where(e => e.StartDate >= from.Value);
        }
    
        if (to.HasValue)
        {
            query = query.Where(e => e.EndDate <= to.Value);
        }
        
        var entities = query.ToList();
        return _mapper.Map<List<EventDto>>(entities);
    }
    
    /// <inheritdoc />
    public EventDto? GetById(int id)
    {
        var entity = Events.FirstOrDefault(e => e.Id == id);
        return entity == null ? null : _mapper.Map<EventDto>(entity);
    }
    
    /// <inheritdoc />
    public EventDto Create(EventSaveDto newEvent)
    {
        // Маппинг SaveDto -> Entity
        var entity = _mapper.Map<EventEntity>(newEvent);
        entity.Id = GenerateNewId();
        
        Events.Add(entity);
        
        // Маппинг Entity -> Dto для ответа
        return _mapper.Map<EventDto>(entity);
    }
    
    /// <inheritdoc />
    public EventDto? Update(int id, EventSaveDto updatedEvent)
    {
        var existingEntity = Events.FirstOrDefault(e => e.Id == id);
        
        if (existingEntity == null)
            return null;

        // Обновляем существующую сущность
        _mapper.Map(updatedEvent, existingEntity);
        
        return _mapper.Map<EventDto>(existingEntity);
    }
    
    /// <inheritdoc />
    public bool Delete(int id)
    {
        return Events.RemoveAll(e => e.Id == id) > 0;
    }
    
    /// <summary>
    /// Генерация идентификатора события
    /// </summary>
    /// <returns>Новый идентификатор</returns>
    private int GenerateNewId()
    {
        return Events.Any() ? Events.Max(e => e.Id) + 1 : 1;
    }
}