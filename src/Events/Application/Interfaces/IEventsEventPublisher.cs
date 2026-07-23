using Contracts.Messages;

namespace Events.Application.Interfaces;

/// <summary>
/// Контракт сервиса для публикации сообщений
/// </summary>
public interface IEventsEventPublisher
{
     /// <summary>
    /// Событие подтверждения бронирования
    /// </summary>
    Task PublishBookingConfirmedAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Событие отклонения бронирования
    /// </summary>
    Task PublishBookingRejectedAsync(
        BookingRejected message,
        CancellationToken cancellationToken = default);
}