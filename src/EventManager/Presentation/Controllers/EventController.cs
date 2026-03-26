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

    /// <summary>
    /// Создание экземпляра класса <see cref="EventController"/>.
    /// </summary>
    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }
    
    /// <summary>
    /// Получить все события
    /// </summary>
    [HttpGet]
    public ActionResult<List<EventDto>> GetAllEvents()
    {
        return _eventService.GetAllEvents();
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