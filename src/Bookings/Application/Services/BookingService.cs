using AutoMapper;
using Bookings.Application.Exceptions;
using Bookings.Application.Interfaces;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;
using Contracts.Auth;
using Contracts.Bookings;

namespace Bookings.Application.Services;

/// <summary>
/// Сервис для обработки бронирования
/// </summary>
public class BookingService : IBookingService
{
    private readonly IMapper _mapper;
    private readonly IBookingRepository _bookingRepository;

    /// <summary>
    /// Конструктор, который принимает зависимости для работы сервиса бронирования
    /// </summary>
    /// <param name="mapper">Экземпляр AutoMapper для преобразования между сущностями и DTO</param>
    /// <param name="bookingRepository">Экземпляр репозитория бронирования для доступа к данным бронирования</param>
    public BookingService(IMapper mapper,
        IBookingRepository bookingRepository)
    {
        _mapper = mapper;
        _bookingRepository = bookingRepository;
    }

    /// <inheritdoc />
    public async Task<BookingDto?> CreateBookingAsync(Guid eventId, Guid userId)
    {
        // TODO потом логика поменяется на сообщения
        /*var eventForBooking = await _eventService.GetById(eventId);
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
       
        
        return _mapper.Map<BookingDto>(entity);*/

        var limitExceeded = await _bookingRepository.HasReachedActiveBookingsLimitAsync(userId);
        if (limitExceeded)
        {
            throw new ActiveBookingsLimitExceededException(_bookingRepository.GetActiveBookingsLimit());
        }

        var entity = new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _bookingRepository.AddAsync(entity);

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

        // TODO потом логика поменяется на сообщения
        /*
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException(userId);
        }

        if (user.Role != Roles.Admin && entity.UserId != userId)
        {
            throw new OperationForbiddenException();
        }*/

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
        // TODO потом логика поменяется на сообщения
        /*var user = await _userService.GetByIdAsync(userId);

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
        }*/

        return bookingDto;
    }
}
