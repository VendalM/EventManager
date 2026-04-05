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
    private static List<BookingEntity> Booking { get; set; } = new();
    private readonly IMapper _mapper;
    private readonly IEventService _eventService;
    
    /// <summary>
    /// Конструктор, который принимает зависимости для работы сервиса бронирования
    /// </summary>
    /// <param name="mapper">Экземпляр AutoMapper для преобразования между сущностями и DTO</param>
    /// <param name="eventService">Экземпляр сервиса событий для проверки существования события перед созданием брони</param>
    public BookingService(IMapper mapper, IEventService eventService)
    {
        _mapper = mapper;
        _eventService = eventService;
    }
    
    /// <inheritdoc />
    public async Task<BookingDto?> CreateBookingAsync(int eventId)
    {
        if (!_eventService.HasEvent(eventId))
        {
            return null;
        }
        
        var entity = new BookingEntity()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.Now
        };
        
        Booking.Add(entity);
        
        return await Task.FromResult(_mapper.Map<BookingDto>(entity));
    }
    
    /// <inheritdoc />
    public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
    {
        var entity = Booking.FirstOrDefault(e => e.Id == bookingId);
        return await Task.FromResult(entity == null ? null : _mapper.Map<BookingDto>(entity));
    }
}
