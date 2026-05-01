using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Exceptions;
using EventManager.Models;

namespace EventManager.Application.Services;

/// <summary>
/// Сервис для обработки событий
/// </summary>
public class EventService : IEventService
{
    private readonly IMapper _mapper;
    private readonly IEventRepository _eventRepository;

    /// <summary>
    /// Конструктор сервиса событий
    /// </summary>
    /// <param name="mapper">AutoMapper для преобразования сущностей</param>
    /// <param name="eventRepository">Экземпляр репозитория событий</param>
    public EventService(IMapper mapper, IEventRepository eventRepository)
    {
        _mapper = mapper;
        _eventRepository = eventRepository;
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
        var entity = await _eventRepository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<EventDto>(entity);
    }
    
    /// <inheritdoc />
    public async Task<EventDto> Create(EventSaveDto newEvent)
    {
        var entity = _mapper.Map<EventEntity>(newEvent);
        entity.Id = Guid.NewGuid();
        entity.AvailableSeats = entity.TotalSeats;
        
        await _eventRepository.AddAsync(entity);
        
        return _mapper.Map<EventDto>(entity);
    }
    
    /// <inheritdoc />
    public async Task<EventDto?> Update(Guid id, EventSaveDto updatedEvent)
    {
        await EventSemaphore.Semaphore.WaitAsync();
        try
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
            
            return _mapper.Map<EventDto>(existingEntity);
        }
        finally
        {
            EventSemaphore.Semaphore.Release();
        }
    }
    
    /// <inheritdoc />
    public async Task<EventDto?> UpdateInternal(Guid id, EventDto updatedEvent)
    {
        var existingEntity = await _eventRepository.GetByIdAsync(id);
        
        if (existingEntity == null)
            return null;
        
        _mapper.Map(updatedEvent, existingEntity);
        await _eventRepository.UpdateAsync(existingEntity);
        
        return _mapper.Map<EventDto>(existingEntity);
    }
    
    /// <inheritdoc />
    public async Task<bool> Delete(Guid id)
    {
        return await _eventRepository.RemoveAsync(id);
    }
    
    /// <inheritdoc />
    public async Task<bool> HasEvent(Guid id)
    {
        return await _eventRepository.HasEventAsync(id);
    }
}