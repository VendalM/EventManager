using Application.Models;

namespace Application.Interfaces;

/// <summary>
/// Контракт для логики сервиса бронирования событий
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание брони для указанного события
    /// </summary>
    /// <param name="eventId">Идентификатор события, для которого создается бронь</param>
    /// <param name="userId">Идентификатор пользователя, создающего бронь.</param>
    Task<BookingDto?> CreateBookingAsync(Guid eventId, Guid userId);
    
    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">Идентификатор брони, которую нужно обработать</param>
    Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid userId);

    /// <summary>
    /// Отмена брони для указанного события
    /// </summary>
    /// <param name="bookingId">Идентификатор события, для которого создается бронь</param>
    /// <param name="userId">Идентификатор пользователя, создающего бронь.</param>
    Task<BookingDto?> CancelBookingAsync(Guid bookingId, Guid userId);
}
