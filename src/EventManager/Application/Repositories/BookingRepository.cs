using EventManager.Application.Interfaces;
using EventManager.Models;

namespace EventManager.Application.Repositories;

/// <summary>
/// Репозиторий для управления данными бронирования событий.
/// </summary>
public class BookingRepository : IBookingRepository
{
    private static readonly List<BookingEntity> Bookings = new();
    
    /// <inheritdoc />
    public Task<BookingEntity?> GetByIdAsync(Guid id)
    {
        var booking = Bookings.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(booking);
    }
    
    /// <inheritdoc />
    public Task AddAsync(BookingEntity booking)
    {
        Bookings.Add(booking);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(BookingEntity newBooking)
    {
        var oldBooking = Bookings.FirstOrDefault(b => b.Id == newBooking.Id);
        if (oldBooking != null)
        {
            oldBooking.EventId = newBooking.EventId;
            oldBooking.Status = newBooking.Status;
            oldBooking.CreatedAt = newBooking.CreatedAt;
            oldBooking.ProcessedAt = newBooking.ProcessedAt;
        }
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<List<BookingEntity>> GetAllAsync()
    {
        return Task.FromResult(Bookings.ToList());
    }
}