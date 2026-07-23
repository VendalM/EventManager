using Contracts.Messages;

namespace Events.Application.Interfaces;

/// <summary>
/// Контракт слушателя сообщений
/// </summary>
public interface IBookingEventHandler
{
    /// <summary>
    /// Заявка на бронирование
    /// </summary>
    Task HandleBookingRequestedAsync(
        BookingRequested message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Бронь отменена
    /// </summary>
    Task HandleBookingCancelledAsync(
        BookingCancelled message,
        CancellationToken cancellationToken = default);
}