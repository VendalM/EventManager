using Contracts.Messages;
using Events.Application.Interfaces;

namespace Events.Application.Services;

/// <summary>
/// Обработчик входящих сообщений о бронировании, которые должны изменить состояние события.
/// </summary>
public class BookingEventHandler : IBookingEventHandler
{
    private readonly IEventService _eventService;
    
    /// <summary>
    /// Конструктор слушателя сообщений
    /// </summary>
    /// <param name="eventService">Экземпляр сервиса событий</param>
    public BookingEventHandler(IEventService eventService)
    {
        _eventService = eventService;
    }
    
    /// <inheritdoc />
    public async Task HandleBookingRequestedAsync(
        BookingRequested message,
        CancellationToken cancellationToken = default)
    {
        await _eventService.BookingRequested(message);
    }

    /// <inheritdoc />
    public async Task HandleBookingCancelledAsync(
        BookingCancelled message,
        CancellationToken cancellationToken = default)
    {
        await _eventService.BookingCancelled(message);
    }
}