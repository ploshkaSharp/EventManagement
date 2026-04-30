using Microsoft.AspNetCore.Mvc;
using EventManagement.DTOs;
using EventManagement.Services;
using EventManagement.Models;

namespace EventManagement.Controllers;

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
  ///   "createdAt": "2026-04-23T10:30:00+04:00",
  ///   "processedAt": null
  /// }
  /// </remarks>
  /// <returns>Информация о бронировании</returns>
  /// <response code="200">Бронирование найдено</response>
  /// <response code="404">Бронирование не найдено</response>
  [HttpGet("{id}")]
  [ProducesResponseType(typeof(BookingDTO), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<BookingDTO>> GetBooking(Guid id)
  {
    var booking = await _bookingService.GetBookingByIdAsync(id);

    if (booking == null)
    {
      return NotFound();
    }

    return Ok(booking);
  }
}