using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Enums;
using EventManager.Models;

namespace EventManager.Application.Services;

/// <summary>
/// Сервис для обработки бронирования
/// </summary>
public class BookingService : IBookingService
{
    /// <summary>
    /// Все доступные бронирования (временное хранилище)
    /// </summary>
    private static List<BookingEntity> Booking { get; set; } = new List<BookingEntity>();
    
    private readonly IMapper _mapper;
    
    /// <summary>
    /// Конструктор сервиса бронирования
    /// </summary>
    /// <param name="mapper">AutoMapper для преобразования сущностей</param>
    public BookingService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    /// <inheritdoc />
    public BookingDto CreateBookingAsync(int eventId)
    {
        var entity = new BookingEntity()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = new DateTime()
        };
        Booking.Add(entity);
        return _mapper.Map<BookingDto>(entity);
    }
    
    /// <inheritdoc />
    public BookingDto? GetBookingByIdAsync(Guid bookingId)
    {
        var entity = Booking.FirstOrDefault(e => e.Id == bookingId);
        return entity == null ? null : _mapper.Map<BookingDto>(entity);
    }
}
