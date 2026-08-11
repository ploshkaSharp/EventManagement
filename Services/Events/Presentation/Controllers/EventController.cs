using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EventManagement.Events.Domain.Entities;
using EventManagement.Events.Application.DTOs;
using EventManagement.Events.Application.Services;
using EventManagement.Events.Domain.Exceptions;
using System.Security.Claims;

namespace EventManagement.Presentation.Controllers;

/// <summary>
/// Контроллер для управления мероприятиями
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    /// <summary>
    /// Контроллер для управления мероприятиями
    /// </summary>        
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnAuthorizedOperationException("GetUserId"));

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
    public async Task<ActionResult<IEnumerable<EventDTO>>> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
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

        var events = await _eventService.GetPaginatedAsync(filter);
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
    public async Task<ActionResult<EventDTO>> GetById(Guid id)
    {
        var eventItem = await _eventService.GetByIdAsync(id);

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
    ///   "startAt": "2026-05-15T10:00:00Z",
    ///   "endAt": "2026-05-15T18:00:00Z",
    ///   "totalSeats" : "200"
    /// }
    /// 
    /// </remarks>
    /// <returns>Созданное мероприятие</returns>
    /// <response code="201">Мероприятие успешно создано</response>
    /// <response code="400">Неверные данные запроса (ошибка валидации)</response>    
    [HttpPost]
    [Authorize(Roles = "Admin")]
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
    ///   "startAt": "2026-06-15T10:00:00Z",
    ///   "endAt": "2026-06-15T18:00:00Z"
    /// }
    /// </remarks>
    /// <returns>Обновленное мероприятие</returns>
    /// <response code="200">Мероприятие успешно обновлено</response>
    /// <response code="400">Неверные данные запроса (ошибка валидации)</response>
    /// <response code="404">Мероприятие с указанным идентификатором не найдено</response>    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateEventDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> Update(Guid id, UpdateEventDTO eventItem)
    {
        try
        {
            var updatedEvent = await _eventService.UpdateAsync(id, eventItem);

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
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _eventService.DeleteAsync(id);

        return NoContent();
    }

    [HttpGet("top")]
    [ProducesResponseType(typeof(IEnumerable<EventDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDTO>>> GetTop10()
    {
        var result = await _eventService.GetTop10Async();
        return Ok(result);
    }
    #endregion
}