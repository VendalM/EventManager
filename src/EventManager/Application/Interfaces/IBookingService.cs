using EventManager.Models;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Контракт для логики сервиса бронирования событий
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание брони для указанного события
    /// </summary>
    /// <param name="eventId">Идентификатор события, для которого создается бронь</param>
    public BookingDto CreateBookingAsync(int eventId);
    
    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">Идентификатор брони, которую нужно обработать</param>
    public BookingDto? GetBookingByIdAsync(Guid bookingId);
}