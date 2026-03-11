using EventManager.Application.Interfaces;
using EventManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;

    EventController(IEventService eventService)
    {
        _eventService = eventService;
    }
    
    [HttpGet]
    public ActionResult<List<Event>> GetAllEvents()
    {
        return _eventService.GetAllEvents();
    }
    
    [HttpGet("{id:int}")]
    public ActionResult<Event> GetById(int id)
    {
        var desiredEvent = _eventService.GetById(id);

        if (desiredEvent == null)
        {
            return NotFound();   
        }
    
        return Ok(desiredEvent);
    }
    
    [HttpPost]
    public IActionResult Create([FromBody] Event value)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        if (value.StartDate >= value.EndDate)
        {
            ModelState.AddModelError("EndDate", "Дата окончания события должна быть больше даты начала.");
            return BadRequest(ModelState);
        }
        
        var createdEvent = _eventService.Create(value);
        
        return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
    }
    
    [HttpPut("{id}")]
    public IActionResult Update(int id,[FromBody] Event updatedEvent)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        if (updatedEvent.StartDate >= updatedEvent.EndDate)
        {
            ModelState.AddModelError("EndDate", "Дата окончания события должна быть больше даты начала.");
            return BadRequest(ModelState);
        }
        
        var result = _eventService.Update(id, updatedEvent);

        if (result)
        {
            return NoContent();
        }
        
        return NotFound();   
    }
    
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        if (_eventService.Delete(id))
        { 
            return NoContent();
        }

        return NotFound();
    }
}