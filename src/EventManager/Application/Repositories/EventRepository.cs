using EventManager.Application.Interfaces;
using EventManager.Infrastructure.DataAccess;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Application.Repositories;

/// <summary>
/// Репозиторий для управления событиями.
/// </summary>
public class EventRepository : IEventRepository
{
    /// <summary>
    /// Контекст базы данных для доступа к данным событий.
    /// </summary>
    private readonly AppDbContext _context;
    
    /// <summary>
    /// Конструктор, который принимает контекст базы данных для взаимодействия с данными событий
    /// </summary>
    public EventRepository(AppDbContext context)
    {
        _context = context;
    }
    
    /// <inheritdoc />
    public async Task<EventEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Events.FirstOrDefaultAsync(b => b.Id == id);
    }
    
    /// <inheritdoc />
    public async Task AddAsync(EventEntity newEvent)
    {
        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(EventEntity newEvent)
    {
        var oldEvent = await _context.Events.FirstOrDefaultAsync(b => b.Id == newEvent.Id);
        if (oldEvent != null)
        {
            oldEvent.StartDate = newEvent.StartDate;
            oldEvent.EndDate = newEvent.EndDate;
            oldEvent.Title = newEvent.Title;
            oldEvent.Description = newEvent.Description;
            oldEvent.TotalSeats = newEvent.TotalSeats;
            oldEvent.AvailableSeats = newEvent.AvailableSeats; 
        }
        await _context.SaveChangesAsync();
    }
    
    /// <inheritdoc />
    public Task<List<EventEntity>> GetAllAsync()
    {
        return _context.Events.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid id)
    {
        var entity = await _context.Events.FindAsync(id);
        if (entity == null)
            return false;

        _context.Events.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public Task<bool> HasEventAsync(Guid id)
    {
        return _context.Events.AnyAsync(e => e.Id == id);
    }
}