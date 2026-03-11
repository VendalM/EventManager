using EventManager.Application.Interfaces;
using EventManager.Models;

namespace EventManager.Application.Services;

/// <summary>
/// Сервис для обработки событий
/// </summary>
public class EventService : IEventService
{
    /// <summary>
    /// Все доступные события
    /// </summary>
    private static List<EventDto> Events { get; set; } = new List<EventDto>();
    
    
    /// <inheritdoc />
    public List<EventDto> GetAllEvents()
    {
        return Events;
    }
    
    /// <inheritdoc />
    public EventDto? GetById(int id)
    {
        return Events.FirstOrDefault(e => e.Id == id);
    }
    
    /// <inheritdoc />
    public EventDto Create(EventSaveDto newEvent)
    {
        newEvent.Id = GenerateNewId();
    
        Events.Add(newEvent);
        return newEvent;
    }
    
    /// <inheritdoc />
    public EventDto? Update(int id, EventSaveDto updatedEvent)
    {
        var existingEvent = Events.FirstOrDefault(e => e.Id == id);
    
        if (existingEvent == null)
            return null;

        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartDate = updatedEvent.StartDate;
        existingEvent.EndDate = updatedEvent.EndDate;
    
        return existingEvent;
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