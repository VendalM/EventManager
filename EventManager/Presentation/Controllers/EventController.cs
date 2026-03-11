using EventManager.Application.Interfaces;
using EventManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Controllers;

/// <summary>
/// Контроллер для работы с событиями
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
            return NotFound(new ProblemDetails
            {
                Title = "Событие не найдено",
                Detail = $"Событие с id {id} не существует",
                Status = StatusCodes.Status404NotFound
            }); 
        }
    
        return Ok(desiredEvent);
    }
    
    /// <summary>
    /// Создает новое событие
    /// </summary>
    [HttpPost]
    public ActionResult<EventDto> Create([FromBody] EventSaveDto value)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }
        
        if (value.StartDate >= value.EndDate)
        {
            ModelState.AddModelError("EndDate", "Дата окончания события должна быть больше даты начала.");
            return ValidationProblem(ModelState);
        }
        
        var createdEvent = _eventService.Create(value);
        
        return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
    }
    
    /// <summary>
    /// Редактирует существующее событие
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<EventDto> Update(int id,[FromBody] EventSaveDto updatedEvent)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }
        
        if (updatedEvent.StartDate >= updatedEvent.EndDate)
        {
            ModelState.AddModelError("EndDate", "Дата окончания события должна быть больше даты начала.");
            return ValidationProblem(ModelState);
        }
        
        var result = _eventService.Update(id, updatedEvent);

        if (result != null)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        
        return NotFound();   
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

        return NotFound(new ProblemDetails
        {
            Title = "Событие не найдено",
            Detail = $"Событие с id {id} не существует",
            Status = StatusCodes.Status404NotFound
        });
    }
}