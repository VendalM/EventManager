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
    public List<EventDto> GetAllEvents();

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
}