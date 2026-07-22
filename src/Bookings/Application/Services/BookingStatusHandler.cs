using Bookings.Application.Interfaces;
using Contracts.Messages;

namespace Bookings.Application.Services;

/// <summary>
/// Обработчик входящих сообщений, которые меняют статус брони после решения сервиса событий.
/// </summary>
public class BookingStatusHandler : IBookingStatusHandler
{
    private readonly IBookingService _bookingService;
    
    /// <summary>
    /// Конструктор слушателя сообщений
    /// </summary>
    /// <param name="bookingService">Экземпляр сервиса бронирования</param>
    public BookingStatusHandler(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <inheritdoc />
    public async Task HandleBookingConfirmedAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        await _bookingService.BookingConfirmed(message);
    }

    /// <inheritdoc />
    public async Task HandleBookingRejectedAsync(
        BookingRejected message,
        CancellationToken cancellationToken = default)
    {
        await _bookingService.BookingRejected(message);
    }
}