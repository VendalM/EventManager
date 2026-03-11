using EventManager.Application.Interfaces;
using EventManager.Models;

namespace EventManager.Application.Services;

public class EventService : IEventService
{
    private static List<Event> Events { get; set; } = new List<Event>();
    
    public List<Event> GetAllEvents()
    {
        return Events;
    }
    
    public Event? GetById(int id)
    {
        return Events.FirstOrDefault(e => e.Id == id);
    }
    
    public Event Create(Event newEvent)
    {
        newEvent.Id = GenerateNewId();
    
        Events.Add(newEvent);
        return newEvent;
    }
    
    public bool Update(int id, Event updatedEvent)
    {
        var existingEvent = Events.FirstOrDefault(e => e.Id == id);
    
        if (existingEvent == null)
            return false;

        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartDate = updatedEvent.StartDate;
        existingEvent.EndDate = updatedEvent.EndDate;
    
        return true;
    }
    
    public bool Delete(int id)
    {
        return Events.RemoveAll(e => e.Id == id) > 0;
    }
    
    private int GenerateNewId()
    {
        return Events.Any() ? Events.Max(e => e.Id) + 1 : 1;
    }
}