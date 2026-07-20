using Application.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

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
    [Authorize]
    [HttpGet("/bookings/{id}")]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var userId = User.GetCurrentUserId();
        var result = await _bookingService.GetBookingByIdAsync(id, userId);
        if (result != null)
        {
            return  Ok(result);
        }

        throw new NotFoundException(id);
    }

    /// <summary>
    /// Отменить бронь по идентификатору
    /// </summary>
    [Authorize]
    [HttpPost("/bookings/{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var userId = User.GetCurrentUserId();
        var result = await _bookingService.CancelBookingAsync(id, userId);
        if (result != null)
        {
            return  Ok(result);
        }

        throw new NotFoundException(id);
    }

    /// <summary>
    /// Отменить бронь.
    /// </summary>
    [Authorize]
    [HttpDelete("/bookings/{id:guid}")]
    public async Task<IActionResult> DeleteBooking(Guid id)
    {
        var userId = User.GetCurrentUserId();
        var result = await _bookingService.CancelBookingAsync(id, userId);
        if (result != null)
        {
            return NoContent();
        }

        throw new NotFoundException(id);
    }
}
