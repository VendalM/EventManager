using EventManager.Models;

namespace EventManager.Application.Interfaces;

public interface IEventService
{
    public List<Event> GetAllEvents();

    public Event? GetById(int id);

    public Event Create(Event newEvent);

    public bool Update(int id, Event updatedEvent);

    public bool Delete(int id);
}