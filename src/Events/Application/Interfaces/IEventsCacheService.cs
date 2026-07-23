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
    Task SetEventById(EventEntity body);

    /// <summary>
    /// Удалить событие из кеша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
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
    Task SetTopEventsAsync(List<EventEntity> events);

    /// <summary>
    /// Удалить топ популярных событий из кеша.
    /// </summary>
    Task RemoveTopEventsAsync();
}