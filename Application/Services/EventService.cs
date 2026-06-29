using EventManagement.Application.DTOs;
using EventManagement.Application.Mappers;
using EventManagement.Application.Ports;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventManagement.Application.Services;

/// <summary>
/// Мероприятие
/// </summary>
public class EventService : IEventService
{  
  private readonly IEventRepository _eventRepository;
  private readonly ILogger<EventService> _logger;
  /// <summary>
  /// 
  /// </summary>
  /// <param name="eventRepository">Репозиторий мероприятий</param>
  /// <param name="logger">Логгер</param>
  public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
  {
    _eventRepository = eventRepository;
    _logger = logger;
  }

  #region === CRUD ===
  /// <summary>
  /// Получить мероприятие по идентификатору
  /// </summary>
  /// <param name="id">Идентификатор мероприятия (GUID)</param>
  /// <returns>DTO мероприятия с указанным идентификатором если оно найдено</returns>  
  public async Task<EventDTO?> GetByIdAsync(Guid id)
  {
    _logger.LogDebug("Retrieving event {EventId}", id);

    var eventItem = await _eventRepository.GetByIdAsync(id);

    if (eventItem == null)
    {
      _logger.LogDebug("Event {EventId} not found", id);
      throw new NotFoundException(nameof(Event), id);
    }

    _logger.LogDebug("Successfully retrieved event {EventId}", id);
    return EventMapper.ToDto(eventItem);
  }

  /// <summary>
  /// Создать новое мероприятие
  /// </summary>
  /// <param name="eventCreatedDTO">Данные для создания мероприятия</param>
  /// <returns>DTO созданного мероприятия</returns>    
  public async Task<EventDTO> CreateAsync(CreateEventDTO eventCreatedDTO)
  {
    _logger.LogInformation("Attempting to create event with title '{Title}'", eventCreatedDTO.Title);

    var existEvents = await _eventRepository.GetAllAsync(new EventFilterDto { Title = eventCreatedDTO.Title });
    
    if (existEvents.Any())
    {
      throw new ValidationException($"Event with title '{eventCreatedDTO.Title}' already exists");
    }

    ValidateEvent(eventCreatedDTO.Title, eventCreatedDTO.StartAt, eventCreatedDTO.EndAt, eventCreatedDTO.TotalSeats);

    var evetItem = new Event(
      eventCreatedDTO.Title,
      eventCreatedDTO.StartAt,
      eventCreatedDTO.EndAt)
    {
      Description = eventCreatedDTO.Description,
      TotalSeats = eventCreatedDTO.TotalSeats,
      AvailableSeats = eventCreatedDTO.TotalSeats
    };

    var createdEvent = await _eventRepository.CreateAsync(evetItem);
    return EventMapper.ToDto(createdEvent);
  }

  /// <summary>
  /// Обновить существующее мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для обновления (GUID)</param>
  /// <param name="eventUpdated">Обновленные данные мероприятия</param>
  /// <returns>DTO обновленного мероприятия если оно найдено</returns>    
  public async Task<EventDTO?> UpdateAsync(Guid id, UpdateEventDTO eventUpdated)
  {
    _logger.LogInformation("Attempting to update event {EventId}", id);

    var eventItem = await _eventRepository.GetByIdAsync(id);

    if (eventItem == null)
    {
      _logger.LogWarning("Failed to update event {EventId}: Event not found", id);
      throw new NotFoundException(nameof(Event), id);
    }

    if (string.IsNullOrEmpty(eventUpdated.Title))
    {
      throw new ValidationException("Title is required.");
    }

    if (eventUpdated.StartAt >= eventUpdated.EndAt)
    {
      throw new ValidationException($"StartAt must be less than EndAt ('{eventUpdated.EndAt}')");
    }

    if (eventUpdated.StartAt < DateTime.UtcNow)
    {
      throw new ValidationException("StartAt must be more than now.");
    }

    eventItem.Title = eventUpdated.Title;
    eventItem.Description = eventUpdated.Description;
    eventItem.StartAt = eventUpdated.StartAt;
    eventItem.EndAt = eventUpdated.EndAt;

    var result = await _eventRepository.UpdateAsync(eventItem);

    return EventMapper.ToDto(eventItem);
  }

  /// <summary>
  /// Удалить мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для удаления (GUID)</param>
  /// <returns>true если удалось удалить</returns>
  public async Task<bool> DeleteAsync(Guid id)
  {
    _logger.LogInformation("Attempting to delete event {EventId}", id);

    var eventItem = await _eventRepository.GetByIdAsync(id);

    if (eventItem == null)
    {
      _logger.LogWarning("Failed to delete event {EventId}: Event not found", id);
      throw new NotFoundException(nameof(Event), id);
    }

    return await _eventRepository.DeleteAsync(id);
  }
  #region    === Валидация ===
  /// <summary>
  /// Валидация полей мероприятия
  /// </summary>
  /// <param name="title">Наименование мероприятия</param>
  /// <param name="startAt">Дата и время начала</param>
  /// <param name="endAt">Дата и время окончания</param>
  /// <param name="totalSeats">Общее количество мест</param>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="ValidationException"></exception>
  private void ValidateEvent(string title, DateTime startAt, DateTime endAt, int totalSeats)
  {
    if (string.IsNullOrEmpty(title))
    {
      throw new ArgumentException("Title is required");
    }

    if (startAt < DateTime.UtcNow)
    {
      throw new ValidationException("StartAt must be more than now.");
    }

    if (startAt >= endAt)
    {
      throw new ValidationException($"StartAt must be less than EndAt");
    }

    if (totalSeats <= 0)
    {
      throw new ValidationException("TotalSeats must be greater than 0");
    }
  }
  #endregion
  #endregion

  #region === Фильтрация ===
  /// <summary>
  /// Получить список всех мероприятий
  /// </summary>
  /// <param name="filter">Параметры фильтра</param>
  /// <returns>Список мероприятий</returns>
  public async Task<IEnumerable<EventDTO>> GetAllAsync(EventFilterDto? filter = null)
  {
    _logger.LogDebug("Retrieving all events with filter");

    var events = await _eventRepository.GetAllAsync(filter);
    return events.Select(EventMapper.ToDto);
  }
  #endregion

  #region === Пагинация ===

  /// <summary>
  /// Получить пагинированный список мероприятий с фильтрацией
  /// </summary>
  /// <param name="filter">Параметры фильтрации и пагинации</param>
  /// <returns>Пагинированный результат с мероприятиями</returns>
  public async Task<PaginatedResult<EventDTO>> GetPaginatedAsync(EventFilterDto filter)
  {
    _logger.LogDebug("Retrieving paginated events - Page {PageNumber}, PageSize {PageSize}", filter.PageNumber, filter.PageSize);

    var paginatedEvents = await _eventRepository.GetPaginatedAsync(filter);

    // пагинированный результат
    return new PaginatedResult<EventDTO>(
        paginatedEvents.Items.Select(EventMapper.ToDto),
        paginatedEvents.TotalCount,
        paginatedEvents.PageNumber,
        paginatedEvents.PageSize
    );
  }
  #endregion

  #region === Бронирование ===
  /// <summary>
  /// Попытка забронировать места на мероприятии
  /// </summary>
  /// <result>true - бронирование удалось, false - не удалось</result>
  public async Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1)
  {
    _logger.LogDebug($"Попытка забронировать {count} мест на мероприятие {eventId}");

    return await _eventRepository.TryReserveSeatsAsync(eventId, count);
  }

  /// <summary>
  /// Вернуть забронированые места
  /// </summary>
  /// <result>true - возврат удался, false - не удалось</result>
  public async Task<bool> ReleaseSeatsAsync(Guid eventId, int count = 1)
  {
    _logger.LogDebug("Attempting to release {Count} seats for event {EventId}", count, eventId);

    return await _eventRepository.ReleaseSeatsAsync(eventId, count);
  }
  #endregion
}