using EventManager.Application.Interfaces;
using EventManager.Exceptions;
using EventManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Controllers;

/// <summary>
/// Контроллер для работы с событиями
/// </summary>
[ApiController]
[Route("/events")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    /// <summary>
    /// Создание экземпляра класса <see cref="EventController"/>.
    /// </summary>
    public EventController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }
    
    /// <summary>
    /// Получить все события
    /// </summary>
    /// @param title Фильтр по названию события (необязательный)
    /// @param from Фильтр по дате начала события (необязательный)
    /// @param to Фильтр по дате окончания события (необязательный)
    /// @param page Номер страницы для пагинации (по умолчанию 1)
    /// @param pageSize Количество элементов на странице для пагинации (по умолчанию 10)
    [HttpGet]
    public ActionResult<PaginatedResult<EventDto>> GetAllEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        return _eventService.GetAllEvents(title, from, to, page, pageSize);
    }
    
    /// <summary>
    /// Получить событие по идентификатору
    /// </summary>
    [HttpGet("{id:int}")]
    public ActionResult<EventDto> GetById(int id)
    {
        var desiredEvent = _eventService.GetById(id);

        if (desiredEvent == null)
        {
            throw new NotFoundException(id);
        }
    
        return Ok(desiredEvent);
    }
    
    /// <summary>
    /// Создает новое событие
    /// </summary>
    [HttpPost]
    public ActionResult<EventDto> Create([FromBody] EventSaveDto value)
    {
        var createdEvent = _eventService.Create(value);
        
        return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
    }
    
    /// <summary>
    /// Редактирует существующее событие
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<EventDto> Update(int id,[FromBody] EventSaveDto updatedEvent)
    {
        if (updatedEvent.StartDate >= updatedEvent.EndDate)
        {
            throw new ValidationException("Дата окончания события должна быть больше даты начала.");
        }
        
        var result = _eventService.Update(id, updatedEvent);

        if (result != null)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        
        throw new NotFoundException(id);
    }
    
    /// <summary>
    /// Создать бронь для события
    /// </summary>
    [HttpPost("/events/{id}/book")]
    public async Task<IActionResult> CreateBooking(int id)
    {
        if (!_eventService.HasEvent(id))
        {
            throw new NotFoundException(id);
        }
        
        var result = await _bookingService.CreateBookingAsync(id);
        
        if (result == null)
        {
            throw new NotFoundException(id);
        }

        HttpContext.Response.Headers.Location = $"/bookings/{result.Id}";
        
        return StatusCode(202, result);
    }
    
    /// <summary>
    /// Удаление
    /// </summary>
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        if (_eventService.Delete(id))
        { 
            return NoContent();
        }

        throw new NotFoundException(id);
    }
}