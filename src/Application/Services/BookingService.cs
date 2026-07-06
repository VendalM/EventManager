using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;
using EventManager.Application.Interfaces;

namespace Application.Services;

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
    public async Task<BookingDto?> CreateBookingAsync(Guid eventId)
    {
        BookingEntity entity;
        
        // Блокируем доступ к ресурсу, чтобы избежать гонки при бронировании последних мест
        await EventSemaphore.Semaphore.WaitAsync();
        try
        {
            var eventForBooking = await _eventService.GetById(eventId);
            if (eventForBooking == null)
            {
                throw new NotFoundException(eventId);
            }
            
            var tryReserve = eventForBooking.TryReserveSeats();
            if (!tryReserve)
            {
                throw new NoAvailableSeatsException(eventForBooking.Title);
            }
            
            await _eventService.UpdateInternal(eventId, eventForBooking);

            entity = new BookingEntity()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        
            await _bookingRepository.AddAsync(entity);
        } 
        finally
        {
            EventSemaphore.Semaphore.Release();
        }
        
        return _mapper.Map<BookingDto>(entity);
    }
    
    /// <inheritdoc />
    public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
    {
        var entity = await _bookingRepository.GetByIdAsync(bookingId);
        return _mapper.Map<BookingDto>(entity);
    }
}
