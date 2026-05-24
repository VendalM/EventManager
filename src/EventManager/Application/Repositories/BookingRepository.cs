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
        booking.CreatedAt = NormalizeToUtc(booking.CreatedAt);
        booking.ProcessedAt = NormalizeToUtc(booking.ProcessedAt);

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
            oldBooking.CreatedAt = NormalizeToUtc(newBooking.CreatedAt);
            oldBooking.ProcessedAt = NormalizeToUtc(newBooking.ProcessedAt);
        }
        await _context.SaveChangesAsync();
    }
    
    /// <inheritdoc />
    public Task<List<BookingEntity>> GetAllAsync()
    {
        return _context.Bookings.ToListAsync();
    }

    /// <summary>
    /// Преобразует указанное значение DateTime в UTC (всемирное координированное время).
    /// </summary>
    /// <param name="value">Исходное значение DateTime.</param>
    /// <returns>
    /// DateTime в формате UTC:
    /// - если входное значение уже имеет Kind = Utc, возвращается как есть;
    /// - если Kind = Local, выполняется преобразование в UTC через <see cref="DateTime.ToUniversalTime"/>;
    /// - если Kind = Unspecified, оно принудительно помечается как Utc без изменения числового значения.
    /// </returns>
    /// <remarks>
    /// Этот метод полезен при записи DateTime в PostgreSQL с типом 'timestamp with time zone',
    /// где допустимы только значения с Kind = Utc. Использование DateTime.Now может привести к ошибкам.
    /// </remarks>
    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Преобразует nullable значение DateTime в UTC (всемирное координированное время).
    /// </summary>
    /// <param name="value">Исходное nullable значение DateTime.</param>
    /// <returns>
    /// Если входное значение не равно null, возвращает результат <see cref="NormalizeToUtc(DateTime)"/>,
    /// иначе возвращает null.
    /// </returns>
    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        return value.HasValue ? NormalizeToUtc(value.Value) : null;
    }
}