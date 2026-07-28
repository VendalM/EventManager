using System.Text.Json;
using Events.Application.Interfaces;
using Events.Domain.Models;
using StackExchange.Redis;

namespace Events.Infrastructure.Services;

/// <summary>
/// Redis-сервис кеширования событий.
/// </summary>
public class EventsCacheService : IEventsCacheService
{
    private readonly IDatabase _db;
    private const string TopEventsKey = "events:top10";
    private readonly ILogger<EventsCacheService> _logger;
    private readonly int _eventsTtlMinutes;
    private readonly int _topEventsTtlMinutes;
    
    /// <summary>
    /// Создает экземпляр <see cref="EventsCacheService"/>.
    /// </summary>
    /// <param name="connection">Подключение к Redis.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <param name="logger">Логгер сервиса кеширования.</param>
    public EventsCacheService(IConnectionMultiplexer connection,
        IConfiguration configuration,
        ILogger<EventsCacheService> logger)
    {
        _db = connection.GetDatabase();
        _logger = logger;

        _eventsTtlMinutes = configuration.GetValue<int>("Cache:EventsTtlMinutes", 10);
        _topEventsTtlMinutes = configuration.GetValue<int>("Cache:TopEventsTtlMinutes", 10);
    }
    
    /// <inheritdoc />
    public async Task<EventEntity?> GetEventById(Guid id)
    {
        try
        {
            RedisValue value = await _db.StringGetAsync(GetEventKey(id));

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<EventEntity>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read event {EventId} from Redis cache.", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetEventById(EventEntity body)
    {
        if (body is null)
        {
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize(body);
            await _db.StringSetAsync(GetEventKey(body.Id), json, TimeSpan.FromMinutes(_eventsTtlMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write event {EventId} to Redis cache.", body.Id);
        }
    }
    
    /// <inheritdoc />
    public async Task RemoveEventAsync(Guid id)
    {
        try
        {
            await _db.KeyDeleteAsync(GetEventKey(id));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove event {EventId} from Redis cache.", id);
        }
    }

    /// <inheritdoc />
    public async Task<List<EventEntity>?> GetTopEventsAsync()
    {
        try
        {
            var value = await _db.StringGetAsync(TopEventsKey);

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<EventEntity>>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read top events from Redis cache.");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetTopEventsAsync(List<EventEntity> events)
    {
        try
        {
            var json = JsonSerializer.Serialize(events);
            await _db.StringSetAsync(TopEventsKey, json, TimeSpan.FromMinutes(_topEventsTtlMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write top events to Redis cache.");
        }
    }

    /// <inheritdoc />
    public async Task RemoveTopEventsAsync()
    {
        try
        {
            await _db.KeyDeleteAsync(TopEventsKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove top events from Redis cache.");
        }
    }

    /// <summary>
    /// Получить ключ события в Redis.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    private static string GetEventKey(Guid id)
    {
        return $"event:{id}";
    }
}