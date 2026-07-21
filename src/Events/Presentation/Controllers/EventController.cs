using System.ComponentModel.DataAnnotations;
using Contracts.Common;
using Contracts.Events;
using Events.Application.Exceptions;
using Events.Application.Interfaces;
using Events.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Presentation.Controllers;

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
    /// @param title Фильтр по названию события (необязательный)
    /// @param from Фильтр по дате начала события (необязательный)
    /// @param to Фильтр по дате окончания события (необязательный)
    /// @param page Номер страницы для пагинации (по умолчанию 1)
    /// @param pageSize Количество элементов на странице для пагинации (по умолчанию 10)
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<EventDto>>> GetAllEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        return await _eventService.GetAllEvents(title, from, to, page, pageSize);
    }
    
    /// <summary>
    /// Получить событие по идентификатору
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        var desiredEvent = await _eventService.GetById(id);

        if (desiredEvent == null)
        {
            throw new NotFoundException(id);
        }
    
        return Ok(desiredEvent);
    }
    
    /// <summary>
    /// Создает новое событие
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] EventSaveDto value)
    {
        var createdEvent = await _eventService.Create(value);
        
        return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
    }
    
    /// <summary>
    /// Редактирует существующее событие
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<EventDto>> Update(Guid id,[FromBody] EventSaveDto updatedEvent)
    {
        if (updatedEvent.StartDate >= updatedEvent.EndDate)
        {
            throw new ValidationException("Дата окончания события должна быть больше даты начала.");
        }
        
        var result = await _eventService.Update(id, updatedEvent);

        if (result != null)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        
        throw new NotFoundException(id);
    }
    
    /// <summary>
    /// Удаление
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (await _eventService.Delete(id))
        { 
            return NoContent();
        }

        throw new NotFoundException(id);
    }
}
