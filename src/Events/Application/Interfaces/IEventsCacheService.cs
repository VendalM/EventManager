using Events.Domain.Models;

namespace Events.Application.Interfaces;

/// <summary>
/// Контракт сервиса кеширования событий.
/// </summary>
public interface IEventsCacheService
{
    /// <summary>
    /// Получить событие из кеша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <returns>Событие из кеша или null, если оно не найдено.</returns>
    Task<EventEntity?> GetEventById(Guid id);

    /// <summary>
    /// Сохранить событие в кеш по идентификатору.
    /// </summary>
    /// <param name="body">Событие для кеширования.</param>
    /// <returns>Задача сохранения события в кеш.</returns>
    Task SetEventById(EventEntity body);

    /// <summary>
    /// Удалить событие из кеша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <returns>Задача удаления события из кеша.</returns>
    Task RemoveEventAsync(Guid id);

    /// <summary>
    /// Получить топ популярных событий из кеша.
    /// </summary>
    /// <returns>Список популярных событий или null, если кеш пуст.</returns>
    Task<List<EventEntity>?> GetTopEventsAsync();

    /// <summary>
    /// Сохранить топ популярных событий в кеш.
    /// </summary>
    /// <param name="events">Список популярных событий.</param>
    /// <returns>Задача сохранения топа популярных событий в кеш.</returns>
    Task SetTopEventsAsync(List<EventEntity> events);

    /// <summary>
    /// Удалить топ популярных событий из кеша.
    /// </summary>
    /// <returns>Задача удаления топа популярных событий из кеша.</returns>
    Task RemoveTopEventsAsync();
}
