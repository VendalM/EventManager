using EventManager.Application.Interfaces;
using EventManager.Models;

namespace EventManager.Application.Repositories;

/// <summary>
/// Репозиторий для управления событиями.
/// </summary>
public class EventRepository : IEventRepository
{
    /// <summary>
    /// Все доступные события (временное хранилище)
    /// </summary>
    private static readonly List<EventEntity> Events = new();
    
    /// <inheritdoc />
    public Task<EventEntity?> GetByIdAsync(Guid id)
    {
        var findEvent = Events.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(findEvent);
    }
    
    /// <inheritdoc />
    public Task AddAsync(EventEntity newEvent)
    {
        Events.Add(newEvent);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(EventEntity newEvent)
    {
        var oldEvent = Events.FirstOrDefault(b => b.Id == newEvent.Id);
        if (oldEvent != null)
        {
            oldEvent.StartDate = newEvent.StartDate;
            oldEvent.EndDate = newEvent.EndDate;
            oldEvent.Title = newEvent.Title;
            oldEvent.Description = newEvent.Description;
            oldEvent.TotalSeats = newEvent.TotalSeats;
            oldEvent.AvailableSeats = newEvent.AvailableSeats; 
        }
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<List<EventEntity>> GetAllAsync()
    {
        return Task.FromResult(Events.ToList());
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(Guid id)
    {
        return Task.FromResult(Events.RemoveAll(e => e.Id == id) > 0);
    }

    /// <inheritdoc />
    public Task<bool> HasEventAsync(Guid id)
    {
        return Task.FromResult(Events.Any(e => e.Id == id));
    }
}