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
    private readonly IUserService _userService;

    /// <summary>
    /// Конструктор, который принимает зависимости для работы сервиса бронирования
    /// </summary>
    /// <param name="mapper">Экземпляр AutoMapper для преобразования между сущностями и DTO</param>
    /// <param name="eventService">Экземпляр сервиса событий для проверки существования события перед созданием брони</param>
    /// <param name="bookingRepository">Экземпляр репозитория бронирования для доступа к данным бронирования</param>
    /// <param name="userService">Экземпляр сервиса для работы с пользователями</param>
    public BookingService(IMapper mapper,
        IEventService eventService,
        IBookingRepository bookingRepository,
        IUserService userService)
    {
        _mapper = mapper;
        _eventService = eventService;
        _bookingRepository = bookingRepository;
        _userService = userService;
    }

    /// <inheritdoc />
    public async Task<BookingDto?> CreateBookingAsync(Guid eventId, Guid userId)
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

            if (eventForBooking.StartDate <= DateTime.UtcNow)
            {
                throw new PastEventBookingException(eventForBooking.Title);
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(userId);
            }

            var limitExceeded = await _bookingRepository.HasReachedActiveBookingsLimitAsync(userId);
            if (limitExceeded)
            {
                throw new ActiveBookingsLimitExceededException(_bookingRepository.GetActiveBookingsLimit());
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
                UserId = userId,
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
    public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid userId)
    {
        var entity = await _bookingRepository.GetByIdAsync(bookingId);
        if (entity == null)
        {
            return null;
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException(userId);
        }

        if (user.Role != Roles.Admin && entity.UserId != userId)
        {
            throw new OperationForbiddenException();
        }

        return _mapper.Map<BookingDto>(entity);
    }

    /// <inheritdoc />
    public async Task<BookingDto?> CancelBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            throw new NotFoundException(bookingId);
        }

        var bookingDto = _mapper.Map<BookingDto>(booking);
        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException(userId);
        }

        if (user.Role != Roles.Admin && booking.UserId != userId)
        {
            throw new OperationForbiddenException();
        }

        if (bookingDto.Status == BookingStatus.Cancelled)
        {
            return bookingDto;
        }

        var shouldReleaseSeat = bookingDto.Status is BookingStatus.Pending or BookingStatus.Confirmed;

        if (user.Role == Roles.Admin || booking.UserId == userId)
        {
            // Блокируем доступ к ресурсу, чтобы избежать гонки при отмене события
            await EventSemaphore.Semaphore.WaitAsync();
            try
            {
                bookingDto.Cancel();
                await _bookingRepository.UpdateAsync(_mapper.Map<BookingEntity>(bookingDto));

                if (shouldReleaseSeat)
                {
                    var eventForBooking = await _eventService.GetById(bookingDto.EventId);
                    if (eventForBooking != null)
                    {
                        eventForBooking.ReleaseSeats();
                        await _eventService.UpdateInternal(bookingDto.EventId, eventForBooking);
                    }
                }
            }
            finally
            {
                EventSemaphore.Semaphore.Release();
            }
        }

        return bookingDto;
    }
}
