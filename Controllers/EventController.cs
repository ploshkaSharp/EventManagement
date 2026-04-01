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
    /// <summary>
    /// Контроллер для управления мероприятиями
    /// </summary>        
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Получить список всех мероприятий
    /// </summary>
    /// <remarks>
    /// Возвращает список всех мероприятий
    /// </remarks>
    /// <returns>Список мероприятий</returns>
    /// <response code="200">Успешно возвращен список мероприятий</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDTO>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<EventDTO>> GetAll()
    {
        var events = _eventService.GetAll();
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
    ///   "startAt": "2026-05-15T10:00:00Z",
    ///   "endAt": "2026-05-15T18:00:00Z"
    /// }
    /// 
    /// </remarks>
    /// <returns>Созданное мероприятие</returns>
    /// <response code="201">Мероприятие успешно создано</response>
    /// <response code="400">Неверные данные запроса (ошибка валидации)</response>    
    [HttpPost]
    [ProducesResponseType(typeof(CreateEventDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]    
    public ActionResult<EventDTO> Create(CreateEventDTO eventItem)
    {     
        try
        {
            var createdEvent = _eventService.Create(eventItem);
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
}