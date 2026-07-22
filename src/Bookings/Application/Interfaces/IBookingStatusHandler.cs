using Contracts.Messages;

namespace Bookings.Application.Interfaces;

/// <summary>
/// Контракт обработчика сообщений о результате обработки брони.
/// </summary>
public interface IBookingStatusHandler
{
    /// <summary>
    /// Обрабатывает сообщение о подтверждении брони.
    /// </summary>
    /// <param name="message">Сообщение о подтверждении брони.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task HandleBookingConfirmedAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обрабатывает сообщение об отклонении брони.
    /// </summary>
    /// <param name="message">Сообщение об отклонении брони.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task HandleBookingRejectedAsync(
        BookingRejected message,
        CancellationToken cancellationToken = default);
}