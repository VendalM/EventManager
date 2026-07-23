using Events.Domain.Models;

namespace Events.Application.Interfaces;

public interface IEventsCacheService
{
    Task<EventEntity?> GetEventById(Guid id);
    Task SetEventById(EventEntity body);
    Task RemoveEventAsync(Guid id);
    Task<List<EventEntity>?> GetTopEventsAsync();
    Task SetTopEventsAsync(List<EventEntity> events);
    Task RemoveTopEventsAsync();
}