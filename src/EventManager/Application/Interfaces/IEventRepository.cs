using EventManager.Models;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Контракт для репозитория, который отвечает за управление данными событий.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Получение событий по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    Task<EventEntity?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Добавление новой брони в репозиторий
    /// </summary>
    /// <param name="booking">Сущность брони, которую нужно добавить в репозиторий</param>
    Task AddAsync(EventEntity booking);
    
    /// <summary>
    /// Обновление существующей брони в репозитории
    /// </summary>
    /// <param name="booking">Сущность брони, которую нужно обновить в репозиторий</param>
    Task UpdateAsync(EventEntity booking);
    
    /// <summary>
    /// Получение всех бронирований
    /// </summary>
    Task<List<EventEntity>> GetAllAsync();
    
    /// <summary>
    /// Удалить события по идентификатору
    /// </summary>
    Task<bool> RemoveAsync(Guid id);

    /// <summary>
    /// Проверить, существует ли событие с данным идентификатором
    /// </summary>
    Task<bool> HasEventAsync(Guid id);
}
