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
    private readonly IMapper _mapper;
    private readonly IEventService _eventService;
    private readonly IBookingRepository _bookingRepository;
    
    /// <summary>
    /// Конструктор, который принимает зависимости для работы сервиса бронирования
    /// </summary>
    /// <param name="mapper">Экземпляр AutoMapper для преобразования между сущностями и DTO</param>
    /// <param name="eventService">Экземпляр сервиса событий для проверки существования события перед созданием брони</param>
    /// <param name="bookingRepository">Экземпляр репозитория бронирования для доступа к данным бронирования</param>
    public BookingService(IMapper mapper, IEventService eventService, IBookingRepository bookingRepository)
    {
        _mapper = mapper;
        _eventService = eventService;
        _bookingRepository = bookingRepository;
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
        
        await _bookingRepository.AddAsync(entity);
        
        return await Task.FromResult(_mapper.Map<BookingDto>(entity));
    }
    
    /// <inheritdoc />
    public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
    {
        var entity = await _bookingRepository.GetByIdAsync(bookingId);
        return await Task.FromResult(entity == null ? null : _mapper.Map<BookingDto>(entity));
    }
}
