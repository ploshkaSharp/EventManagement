using Microsoft.AspNetCore.Mvc;
using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Services;

namespace EventManagement.Controllers;

/// <summary>
/// Контроллер для управления мероприятиями
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    /// <summary>
    /// Контроллер для управления мероприятиями
    /// </summary>        
    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }

    #region === Мероприятия ===
    /// <summary>
    /// Получить список всех мероприятий
    /// </summary>
    /// <param name="title">Фильтр по названию (регистронезависимый, частичное совпадение)</param>
    /// <param name="from">Фильтр по дате начала (события, начинающиеся не раньше указанной даты)</param>
    /// <param name="to">Фильтр по дате окончания (события, заканчивающиеся не позже указанной даты)</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Размер элементов на странице</param>
    /// <remarks>
    /// Возвращает список всех мероприятий
    /// </remarks>
    /// <returns>Список мероприятий</returns>
    /// <response code="200">Успешно возвращен список мероприятий</response>
    /// <response code="400">Неверные параметры фильтрации</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDTO>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<EventDTO>> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var filter = new EventFilterDto
        {
            Title = title,
            From = from,
            To = to,
            PageNumber = page,
            PageSize = pageSize
        };

        var events = _eventService.GetPaginated(filter);
        return Ok(events);
    }

    /// <summary>
    /// Получить мероприятие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор мероприятия (GUID)</param>
    /// <remarks>
    /// Возвращает мероприятие по указанному идентификатору
    /// </remarks>
    /// <returns>Мероприятие с указанным идентификатором</returns>
    /// <response code="200">Мероприятие найдено и успешно возвращено</response>
    /// <response code="404">Мероприятие с указанным идентификатором не найдено</response>    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EventDTO> GetById(Guid id)
    {
        var eventItem = _eventService.GetById(id);

        if (eventItem == null)
        {
            return NotFound();
        }

        return Ok(eventItem);
    }

    /// <summary>
    /// Создать новое мероприятие
    /// </summary>
    /// <param name="eventItem">Данные для создания мероприятия</param>
    /// <remarks>
    /// Пример запроса:
    /// POST /events
    /// {
    ///   "title": "Tech Conference 2026",
    ///   "description": "Annual technology conference",
    ///   "startAt": "2026-05-15T10:00:00+04:00",
    ///   "endAt": "2026-05-15T18:00:00+04:00"
    /// }
    /// 
    /// </remarks>
    /// <returns>Созданное мероприятие</returns>
    /// <response code="201">Мероприятие успешно создано</response>
    /// <response code="400">Неверные данные запроса (ошибка валидации)</response>    
    [HttpPost]
    [ProducesResponseType(typeof(CreateEventDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDTO>> Create([FromBody] CreateEventDTO eventItem)
    {
        try
        {
            var createdEvent = await _eventService.CreateAsync(eventItem);
            return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Обновить существующее мероприятие
    /// </summary>
    /// <param name="id">Идентификатор мероприятия для обновления (GUID)</param>
    /// <param name="eventItem">Обновленные данные мероприятия</param>
    /// <remarks>
    /// Пример запроса:
    /// PUT /events/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// {
    ///   "title": "Updated Conference 2026",
    ///   "description": "Updated technology conference",
    ///   "startAt": "2026-06-15T10:00:00+04:00",
    ///   "endAt": "2026-06-15T18:00:00+04:00"
    /// }
    /// </remarks>
    /// <returns>Обновленное мероприятие</returns>
    /// <response code="200">Мероприятие успешно обновлено</response>
    /// <response code="400">Неверные данные запроса (ошибка валидации)</response>
    /// <response code="404">Мероприятие с указанным идентификатором не найдено</response>    
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UpdateEventDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Event> Update(Guid id, UpdateEventDTO eventItem)
    {
        try
        {
            var updatedEvent = _eventService.Update(id, eventItem);

            if (updatedEvent == null)
            {
                return NotFound();
            }

            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Удалить мероприятие
    /// </summary>
    /// <param name="id">Идентификатор мероприятия для удаления (GUID)</param>
    /// <remarks>
    /// Пример запроса:
    /// DELETE /events/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// 
    /// При успешном удалении возвращается статус 204 No Content
    /// </remarks>
    /// <returns>Статус выполнения операции</returns>
    /// <response code="204">Мероприятие успешно удалено</response>
    /// <response code="404">Мероприятие с указанным идентификатором не найдено</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        var deleted = _eventService.Delete(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
    #endregion

    #region === Бронирование ===
    /// <summary>
    /// Создать бронирование на мероприятие
    /// </summary>
    /// <param name="id">Идентификатор мероприятия (GUID)</param>
    /// <remarks>
    /// Пример запроса:
    /// POST /events/fd1c1927-dd18-4e08-bc6f-a5517290d729/book
    /// 
    /// Пример ответа:
    /// {
    ///   "id": "06643d61-2689-49df-aa08-42c0ab9a8577",
    ///   "eventId": "fd1c1927-dd18-4e08-bc6f-a5517290d729",
    ///   "status": 0,
    ///   "createdAt": "2026-04-23T10:30:00+04:00",
    ///   "processedAt": null
    /// }
    /// </remarks>
    /// <returns>Информация о созданной брони</returns>
    /// <response code="202">Бронирование успешно создано и принято в обработку</response>
    /// <response code="404">Мероприятие не найдено</response>
    /// <response code="400">Невозможно создать бронирование (мероприятие уже началось)</response>
    /// <response code="409">Нет свободных мест на мероприятии</response>
    [HttpPost("{id}/book")]
    [ProducesResponseType(typeof(BookingDTO), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]    
    public async Task<ActionResult<BookingDTO>> BookEvent(Guid id)
    {
        // Проверить существование мероприятия
        var eventItem = _eventService.GetById(id);

        if (eventItem == null)
        {
          return NotFound();
        }

        if (eventItem.StartAt < DateTimeOffset.Now)
        {
          return BadRequest("Can not book an event that has already started");
        }

        if (eventItem.AvailableSeats <= 0)
        {
          return Conflict($"No available seats for event '{eventItem.Title}'");
        }        

        // Создать бронь
        var booking = await _bookingService.CreateBookingAsync(id);

        // Вернуть 202 Accepted с Location header
        return AcceptedAtAction(
            "GetBooking",
            "Bookings",
            new { id = booking.Id },
            booking);
    }
    #endregion
}