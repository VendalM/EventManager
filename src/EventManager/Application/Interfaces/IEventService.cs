using EventManager.Models;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Контракт для логики сервиса обработки событий
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить все доступные события
    /// </summary>
    /// <returns>События</returns>
    /// @param title Фильтр по названию события (необязательный)
    /// @param from Фильтр по дате начала события (необязательный)
    /// @param to Фильтр по дате окончания события (необязательный)
    /// @param page Номер страницы для пагинации (по умолчанию 1)
    /// @param pageSize Количество элементов на странице для пагинации (по умолчанию 10)
    public PaginatedResult<EventDto> GetAllEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);

    /// <summary>
    /// Получить событие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <returns>Найденное событие</returns>
    public EventDto? GetById(int id);

    /// <summary>
    /// Создать событие
    /// </summary>
    /// <param name="newEvent">Новое событие</param>
    /// <returns>Созданное событие</returns>
    public EventDto Create(EventSaveDto newEvent);

    /// <summary>
    /// Обновить событие
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="updatedEvent">Обновляемое событие</param>
    /// <returns>Удалось ли обновить событие?</returns>
    public EventDto? Update(int id, EventSaveDto updatedEvent);

    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <returns>Удалось ли удалить событие?</returns>
    public bool Delete(int id);
    
    /// <summary>
    /// Проверить, существует ли событие с данным идентификатором
    /// </summary>
    /// <param name="id">Идентификатор</param>
    public bool HasEvent(int id);
}