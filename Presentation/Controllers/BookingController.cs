using Microsoft.AspNetCore.Mvc;
using EventManagement.Application.DTOs;
using EventManagement.Application.Services;
using System.Security.Claims;
using EventManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace EventManagement.Presentation.Controllers;

/// <summary>
/// Контроллер для управления бронированиями
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
  private readonly IBookingService _bookingService;
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="bookingService"></param>
  public BookingsController(IBookingService bookingService)
  {
    _bookingService = bookingService;
  }

  private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnAuthorizedOperationException("GetUserId"));
  private bool IsAdmin() => User.IsInRole("Admin");

  /// <summary>
  /// Получить бронирование по идентификатору
  /// </summary>
  /// <param name="id">Идентификатор бронирования (GUID)</param>
  /// <remarks>
  /// Пример запроса:
  /// GET /bookings/fd1c1927-dd18-4e08-bc6f-a5517290d729
  /// 
  /// Пример ответа (бронь в статусе Pending):
  /// {
  ///   "id": "fd1c1927-dd18-4e08-bc6f-a5517290d729",
  ///   "eventId": "06643d61-2689-49df-aa08-42c0ab9a8577",
  ///   "status": 0,
  ///   "createdAt": "2026-04-23T10:30:00Z",
  ///   "processedAt": null
  /// }
  /// </remarks>
  /// <returns>Информация о бронировании</returns>
  /// <response code="200">Бронирование найдено</response>
  /// <response code="404">Бронирование не найдено</response>
  [Authorize]
  [HttpGet("{id}")]
  [ProducesResponseType(typeof(BookingDTO), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<BookingDTO>> GetBooking(Guid id)
  {
    var userId = GetUserId();
    var isAdmin = IsAdmin();
    var booking = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin);

    if (booking == null)
    {
      return NotFound();
    }

    return Ok(booking);
  }

  /// <summary>
  /// Отмена брони
  /// </summary>
  /// <param name="id">ИД бронирования</param> 
  [Authorize] 
  [HttpDelete("{id}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]  
  public async Task<IActionResult> Cancel(Guid id)
  {
    var userId = GetUserId();
    var isAdmin = IsAdmin();
    await _bookingService.CancelBookingAsync(id, userId, isAdmin);
    return NoContent();
  }
}