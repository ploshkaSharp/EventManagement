using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Mappers;
using EventManagement.Exceptions;
using EventManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Services;

/// <summary>
/// Мероприятие
/// </summary>
public class EventService : IEventService
{
  private readonly AppDbContext _context;
  private readonly ILogger<EventService> _logger;
  /// <summary>
  /// 
  /// </summary>
  /// <param name="context">Контекст БД</param>
  /// <param name="logger">Логгер</param>
  public EventService(AppDbContext context, ILogger<EventService> logger)
  {
    _context = context;
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
    
    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);

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
  /// <param name="eventCreated">Данные для создания мероприятия</param>
  /// <returns>DTO созданного мероприятия</returns>    
  public async Task<EventDTO> CreateAsync(CreateEventDTO eventCreated)
  {
    _logger.LogInformation("Attempting to create event with title '{Title}'", eventCreated.Title);
    var eventItem = EventMapper.ToEntity(eventCreated);

    var isExistEvent = await _context.Events.FirstOrDefaultAsync(e => e.Title == eventCreated.Title);

    if (isExistEvent != null)
    {
      throw new ValidationException($"Event with title '{eventCreated.Title}' already exists");
    }

    ValidateEvent(eventCreated.Title, eventCreated.StartAt, eventCreated.EndAt, eventCreated.TotalSeats);

    var evetItem = new Event(
      eventCreated.Title,
      eventCreated.StartAt,
      eventCreated.EndAt)
    {
      Description = eventCreated.Description,
      TotalSeats = eventCreated.TotalSeats
    };

    _context.Events.Add(eventItem);
    await _context.SaveChangesAsync();

    return EventMapper.ToDto(eventItem);
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

    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        
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
        
    await _context.SaveChangesAsync();

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
    
    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        
    if (eventItem == null)
    {
      _logger.LogWarning("Failed to delete event {EventId}: Event not found", id);
      throw new NotFoundException(nameof(Event), id);
    }

    _context.Events.Remove(eventItem);
    await _context.SaveChangesAsync();

    return true;
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

    if (filter != null && filter.From != null && filter.To != null && filter.From > filter.To)
    {
      throw new BadRequestException("'from' cannot be more than 'to'");
    }

    var query = _context.Events.AsQueryable();

    if (filter != null)
    {
      if (!string.IsNullOrWhiteSpace(filter.Title))
      {
        query = query.Where(e => e.Title.ToLower().Contains(filter.Title));
      }

      if (filter.From.HasValue)
      {
        query = query.Where(e => e.StartAt >= filter.From.Value);
      }

      if (filter.To.HasValue)
      {
        query = query.Where(e => e.EndAt <= filter.To.Value);
      }      
    }

    var events = await query.OrderBy(e => e.StartAt).ToListAsync();
    return EventMapper.ToDtoList(events);
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
    
    filter.Validate();

    var query =  _context.Events.AsQueryable();

    if (filter != null)
    {
      if (!string.IsNullOrWhiteSpace(filter.Title))
      {
        query = query.Where(e => e.Title.ToLower().Contains(filter.Title));
      }

      if (filter.From.HasValue)
      {
        query = query.Where(e => e.StartAt >= filter.From.Value);
      }

      if (filter.To.HasValue)
      {
        query = query.Where(e => e.EndAt <= filter.To.Value);
      }      
    }    

    // Общее количество записей ДО пагинации
    var totalCount = await query.CountAsync();

    // Элементы для текущей страницы
    var items = await query
        .OrderBy(e => e.StartAt) // Сортировка для консистентности
        .Skip((filter!.PageNumber - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync();

    var itemDtos = EventMapper.ToDtoList(items);

    // пагинированный результат
    return new PaginatedResult<EventDTO>(
        itemDtos,
        totalCount,
        filter.PageNumber,
        filter.PageSize
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

    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);

    if (eventItem == null)
    {
      _logger.LogDebug($"Мероприятие {eventId} не найдено");
      return false;
    }
    
    var result = eventItem.TryReserveSeats(count);

    if (result)
    {
      await _context.SaveChangesAsync();
      _logger.LogDebug($"Успешно забронировано {count} мест на мероприятие {eventId}. Отсалось доступных мест: {eventItem.AvailableSeats}");
    }
    else
    {
      _logger.LogDebug($"Не удалось забронировать {count} мест на мероприятие {eventId}. Только {eventItem.AvailableSeats} доступных мест для бронирования.");
    }

    return result;
  }

  /// <summary>
  /// Вернуть забронированые места
  /// </summary>
  /// <result>true - возврат удался, false - не удалось</result>
  public async Task<bool> ReleaseSeatsAsync(Guid eventId, int count = 1)
  {
    _logger.LogDebug("Attempting to release {Count} seats for event {EventId}", count, eventId);

    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);

    if (eventItem != null)
    {
      eventItem.ReleaseSeats(count);
      await _context.SaveChangesAsync();
    }
    else
    {
      return false;
    }
    return true;
  }
  #endregion
}