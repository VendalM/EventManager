using AutoMapper;
using Events.Domain.Exceptions;
using Contracts.Common;
using Contracts.Enums;
using Contracts.Events;
using Contracts.Messages;
using Events.Application.Exceptions;
using Events.Application.Interfaces;
using Events.Application.Models;
using Events.Domain.Models;

namespace Events.Application.Services;

/// <summary>
/// Сервис для обработки событий
/// </summary>
public class EventService : IEventService
{
    private readonly IMapper _mapper;
    private readonly IEventRepository _eventRepository;
    private readonly IEventsEventPublisher _eventsEventPublisher;
    private readonly IConfiguration _configuration;
    private readonly IEventsCacheService _cacheService;
    private readonly int _maxTopic;

    /// <summary>
    /// Конструктор сервиса событий
    /// </summary>
    /// <param name="mapper">AutoMapper для преобразования сущностей</param>
    /// <param name="eventRepository">Экземпляр репозитория событий</param>
    /// <param name="eventsEventPublisher">Экземпляр контракта отправки сообщений</param>
    /// <param name="configuration">Конфигурация приложения</param>
    /// <param name="cacheService">Сервис кеширования</param>
    public EventService(IMapper mapper, 
        IEventRepository eventRepository, 
        IEventsEventPublisher eventsEventPublisher,
        IConfiguration configuration,
        IEventsCacheService cacheService)
    {
        _mapper = mapper;
        _eventRepository = eventRepository;
        _eventsEventPublisher = eventsEventPublisher;
        _configuration = configuration;
        _cacheService = cacheService;

        _maxTopic = _configuration.GetValue<int>("Options:MaxTopic", 10);
    }
    
    /// <inheritdoc />
    public async Task<PaginatedResult<EventDto>> GetAllEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var query = (await _eventRepository.GetAllAsync()).AsQueryable();
        
        if (!string.IsNullOrEmpty(title))
        {
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }
    
        if (from.HasValue)
        {
            query = query.Where(e => e.StartDate >= from.Value);
        }
    
        if (to.HasValue)
        {
            query = query.Where(e => e.EndDate <= to.Value);
        }
        
        var totalItems = query.Count();
        
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return new PaginatedResult<EventDto>()
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = _mapper.Map<List<EventDto>>(items)
        };
    }
    
    /// <inheritdoc />
    public async Task<EventDto?> GetById(Guid id)
    {
        var entity = await _cacheService.GetEventById(id);

        if (entity is not null)
        {
            return _mapper.Map<EventDto>(entity);
        }

        entity = await _eventRepository.GetByIdAsync(id);

        if (entity is null)
        {
            return null;
        }
        
        await _cacheService.SetEventById(entity);

        return _mapper.Map<EventDto>(entity);
    }
    
    /// <inheritdoc />
    public async Task<EventDto> Create(EventSaveDto newEvent)
    {
        var entity = _mapper.Map<EventEntity>(newEvent);
        entity.Id = Guid.NewGuid();
        entity.AvailableSeats = entity.TotalSeats;
        
        await _eventRepository.AddAsync(entity);

        await _cacheService.SetEventById(entity);
        return _mapper.Map<EventDto>(entity);
    }
    
    /// <inheritdoc />
    public async Task<EventDto?> Update(Guid id, EventSaveDto updatedEvent)
    {
        var existingEntity = await _eventRepository.GetByIdAsync(id);

        if (existingEntity == null)
            return null;

        var occupiedSeats = existingEntity.TotalSeats - existingEntity.AvailableSeats;
        _mapper.Map(updatedEvent, existingEntity);

        if (updatedEvent.TotalSeats.HasValue)
        {
            var newTotal = existingEntity.TotalSeats;

            if (newTotal < occupiedSeats)
            {
                throw new TotalSeatsTooLowException(existingEntity.Title, newTotal, occupiedSeats);
            }

            existingEntity.AvailableSeats = newTotal - occupiedSeats;
        }

        await _eventRepository.UpdateAsync(existingEntity);
            
        await _cacheService.RemoveEventAsync(id);
        await _cacheService.SetEventById(existingEntity);
        return _mapper.Map<EventDto>(existingEntity);
    }
    
    /// <inheritdoc />
    public async Task<EventDto?> UpdateInternal(Guid id, EventDto updatedEvent)
    {
        var existingEntity = await _eventRepository.GetByIdAsync(id);
        
        if (existingEntity == null)
            return null;
        
        _mapper.Map(updatedEvent, existingEntity);
        await _eventRepository.UpdateAsync(existingEntity);
        
        await _cacheService.RemoveEventAsync(id);
        await _cacheService.SetEventById(existingEntity);
        return _mapper.Map<EventDto>(existingEntity);
    }
    
    /// <inheritdoc />
    public async Task<bool> Delete(Guid id)
    {
        var removed = await _eventRepository.RemoveAsync(id);

        if (removed)
        {
            await _cacheService.RemoveEventAsync(id);
        }

        return removed;
    }
    
    /// <inheritdoc />
    public async Task<bool> HasEvent(Guid id)
    {
        return await _eventRepository.HasEventAsync(id);
    }
    
    /// <inheritdoc />
    public async Task BookingRequested(BookingRequested body)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(body.EventId);
        if (eventEntity == null)
        {
            await _eventsEventPublisher.PublishBookingRejectedAsync(
                new BookingRejected(
                    body.BookingId,
                    body.EventId,
                    body.UserId,
                    body.SeatsCount,
                    BookingRejectionReason.EventNotFound,
                    DateTime.UtcNow));
            return;
        }

        if (eventEntity.StartDate <= DateTime.UtcNow)
        {
            await _eventsEventPublisher.PublishBookingRejectedAsync(
                new BookingRejected(
                    body.BookingId,
                    body.EventId,
                    body.UserId,
                    body.SeatsCount,
                    BookingRejectionReason.EventAlreadyStarted,
                    DateTime.UtcNow));
            return;
        }

        var tryReserve = eventEntity.TryReserveSeats(body.SeatsCount);
        if (!tryReserve)
        {
            await _eventsEventPublisher.PublishBookingRejectedAsync(
                new BookingRejected(
                    body.BookingId,
                    body.EventId,
                    body.UserId,
                    body.SeatsCount,
                    BookingRejectionReason.NoAvailableSeats,
                    DateTime.UtcNow));
            return;
        }
            
        await _eventRepository.UpdateAsync(eventEntity);

        await _eventsEventPublisher.PublishBookingConfirmedAsync(
            new BookingConfirmed(
                body.BookingId,
                body.EventId,
                body.UserId,
                body.SeatsCount,
                DateTime.UtcNow));
        
        await _cacheService.RemoveEventAsync(body.EventId);
        await _cacheService.SetEventById(eventEntity);
    }
    
    /// <inheritdoc />
    public async Task BookingCancelled(BookingCancelled body)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(body.EventId);
        if (eventEntity == null)
        {
            return;
        }
        
        eventEntity.ReleaseSeats(body.SeatsCount);
      
        await _eventRepository.UpdateAsync(eventEntity);
        await _cacheService.RemoveEventAsync(body.EventId);
        await _cacheService.SetEventById(eventEntity);
    }

    /// <inheritdoc />
    public async Task<List<EventDto>?> GetTopEventsCachedAsync()
    {
        var entity = await _cacheService.GetTopEventsAsync();
        
        if (entity is not null)
        {
            return _mapper.Map<List<EventDto>>(entity);
        }
        
        var topEvents = await _eventRepository.GetTopEventsAsync(_maxTopic);
        
        if (topEvents is not null)
        {
            await _cacheService.SetTopEventsAsync(topEvents); 
        }

        return _mapper.Map<List<EventDto>>(topEvents);
    }
}
