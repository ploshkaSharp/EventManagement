using Microsoft.AspNetCore.Mvc;
using EventManagement.Bookings.Application.DTOs;
using EventManagement.Bookings.Application.Services;
using System.Security.Claims;
using EventManagement.Bookings.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace EventManagement.Bookings.Presentation.Controllers;

/// <summary>
/// Контроллер для управления бронированиями
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bookingService"></param>
    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnAuthorizedOperationException("GetUserId"));
    private bool IsAdmin() => User.IsInRole("Admin");

    /// <summary>
    /// Создать новое бронирование
    /// </summary>
    /// <param name="createDto">Данные для создания бронирования</param>
    /// <returns>Информация о созданном бронировании</returns>
    /// <response code="201">Бронирование успешно создано</response>
    /// <response code="400">Неверные данные запроса</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="404">Событие или пользователь не найдены</response>
    /// <response code="409">Достигнут лимит броней или нет свободных мест</response>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(BookingDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDTO>> CreateBooking([FromBody] CreateBookingDTO createDto)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} creating booking for event {EventId}", userId, createDto.EventId);

            var booking = await _bookingService.CreateBookingAsync(createDto.EventId, userId);

            _logger.LogInformation("Booking {BookingId} created successfully for user {UserId}", booking.Id, userId);

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            return NotFound(new ProblemDetails
            {
                Title = "Resource Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (BookingLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Booking limit exceeded: {Message}", ex.Message);
            return Conflict(new ProblemDetails
            {
                Title = "Booking Limit Exceeded",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for event {EventId}", createDto.EventId);
            throw;
        }
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