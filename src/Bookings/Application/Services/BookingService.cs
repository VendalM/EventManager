using AutoMapper;
using Bookings.Application.Exceptions;
using Bookings.Application.Interfaces;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;
using Contracts.Bookings;
using Contracts.Messages;

namespace Bookings.Application.Services;

/// <summary>
/// Сервис для обработки бронирования
/// </summary>
public class BookingService : IBookingService
{
    private readonly IMapper _mapper;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingEventPublisher _bookingEventPublisher;

    /// <summary>
    /// Конструктор, который принимает зависимости для работы сервиса бронирования
    /// </summary>
    /// <param name="mapper">Экземпляр AutoMapper для преобразования между сущностями и DTO</param>
    /// <param name="bookingRepository">Экземпляр репозитория бронирования для доступа к данным бронирования</param>
    ///  /// <param name="bookingEventPublisher">Экземпляр сервиса для публикации событий. </param>
    public BookingService(IMapper mapper,
        IBookingRepository bookingRepository,
        IBookingEventPublisher bookingEventPublisher)
    {
        _mapper = mapper;
        _bookingRepository = bookingRepository;
        _bookingEventPublisher = bookingEventPublisher;
    }

    /// <inheritdoc />
    public async Task<BookingDto?> CreateBookingAsync(Guid eventId, Guid userId)
    {
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

        await _bookingEventPublisher.PublishBookingRequestedAsync(
            new BookingRequested(
                entity.Id,
                entity.EventId,
                entity.UserId,
                1,
                entity.CreatedAt));
        
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
        
        booking.Cancel();
        await _bookingRepository.UpdateAsync(booking);
        
        await _bookingEventPublisher.PublishBookingCancelledAsync(
            new BookingCancelled(
                booking.Id,
                booking.EventId,
                booking.UserId,
                1,
                booking.ProcessedAt.Value));

        return _mapper.Map<BookingDto>(booking);
    }
}
