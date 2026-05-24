using EventManager.Application.Interfaces;
using EventManager.Infrastructure.DataAccess;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Application.Repositories;

/// <summary>
/// Репозиторий для управления данными бронирования событий.
/// </summary>
public class BookingRepository : IBookingRepository
{
    /// <summary>
    /// Контекст базы данных для доступа к данным бронирования.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Конструктор, который принимает контекст базы данных для взаимодействия с данными бронирования
    /// </summary>
    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }
    
    /// <inheritdoc />
    public Task<BookingEntity?> GetByIdAsync(Guid id)
    {
        return _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }
    
    /// <inheritdoc />
    public async Task AddAsync(BookingEntity booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(BookingEntity newBooking)
    {
        var oldBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == newBooking.Id);
        if (oldBooking != null)
        {
            oldBooking.EventId = newBooking.EventId;
            oldBooking.Status = newBooking.Status;
            oldBooking.CreatedAt = newBooking.CreatedAt;
            oldBooking.ProcessedAt = newBooking.ProcessedAt;
        }
        await _context.SaveChangesAsync();
    }
    
    /// <inheritdoc />
    public Task<List<BookingEntity>> GetAllAsync()
    {
        return _context.Bookings.ToListAsync();
    }
}