using EventManager.Models;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Контракт для репозитория, который отвечает за управление данными бронирования событий
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    Task<BookingEntity?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Добавление новой брони в репозиторий
    /// </summary>
    /// <param name="booking">Сущность брони, которую нужно добавить в репозиторий</param>
    Task AddAsync(BookingEntity booking);
    
    /// <summary>
    /// Обновление существующей брони в репозитории
    /// </summary>
    /// <param name="booking">Сущность брони, которую нужно обновить в репозиторий</param>
    Task UpdateAsync(BookingEntity booking);
    
    /// <summary>
    /// Получение всех бронирований
    /// </summary>
    Task<List<BookingEntity>> GetAllAsync();
}
