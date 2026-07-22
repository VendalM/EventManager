using Contracts.Messages;

namespace Bookings.Application.Interfaces;

/// <summary>
/// Контракт сервиса для публикации сообщений
/// </summary>
public interface IBookingEventPublisher
{
    /// <summary>
    /// Событие запроса бронирования
    /// </summary>
    Task PublishBookingRequestedAsync(
        BookingRequested message,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Событие отмены бронирования
    /// </summary>
    Task PublishBookingCancelledAsync(
        BookingCancelled message,
        CancellationToken cancellationToken = default);
}