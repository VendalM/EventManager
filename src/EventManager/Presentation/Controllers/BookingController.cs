using EventManager.Application.Interfaces;
using EventManager.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Controllers;

/// <summary>
/// Контроллер для работы с бронированиями
/// </summary>
[ApiController]
[Route("/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    /// <summary>
    /// Создание экземпляра класса <see cref="EventController"/>.
    /// </summary>
    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }
    
    /// <summary>
    /// Получить бронь по идентификатору
    /// </summary>
    [HttpPost("/bookings/{id}")]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var result = await _bookingService.GetBookingByIdAsync(id);
        if (result != null)
        {
            return CreatedAtAction(nameof(GetBooking), new { id = result.Id }, result);
        }

        throw new NotFoundException(id);
    }
}
