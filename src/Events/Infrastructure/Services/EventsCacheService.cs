using System.Text.Json;
using Events.Application.Interfaces;
using Events.Domain.Models;
using StackExchange.Redis;

namespace Events.Infrastructure.Services;

public class EventsCacheService : IEventsCacheService
{
    private readonly IDatabase _db;
    private const string TopEventsKey = "events:top10";
    private static readonly TimeSpan TopEventsTtl = TimeSpan.FromMinutes(10);
    
    public EventsCacheService(IConnectionMultiplexer connection)
    {
        _db = connection.GetDatabase();
    }
    
    /// <inheritdoc />
    public async Task<EventEntity?> GetEventById(Guid id)
    {
        RedisValue value = await _db.StringGetAsync($"events:{id}");

        if (!value.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<EventEntity>(value.ToString()); 
    }

    /// <inheritdoc />
    public async Task SetEventById(EventEntity body)
    {
        if (body is null)
        {
            return;
        }
        
        string json = JsonSerializer.Serialize(body);
        await _db.StringSetAsync($"events:{body.Id}", json, TimeSpan.FromMinutes(10));
    }
    
    /// <inheritdoc />
    public Task RemoveEventAsync(Guid id)
    {
        return _db.KeyDeleteAsync($"events:{id}");
    }

    /// <inheritdoc />
    public async Task<List<EventEntity>?> GetTopEventsAsync()
    {
        var value = await _db.StringGetAsync(TopEventsKey);

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<List<EventEntity>>(value.ToString());
    }

    /// <inheritdoc />
    public Task SetTopEventsAsync(List<EventEntity> events)
    {
        var json = JsonSerializer.Serialize(events);
        return _db.StringSetAsync(TopEventsKey, json, TopEventsTtl);
    }

    /// <inheritdoc />
    public Task RemoveTopEventsAsync()
    {
        return _db.KeyDeleteAsync(TopEventsKey);
    }
}