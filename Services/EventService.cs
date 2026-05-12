using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Mappers;
using EventManagement.Exceptions;

namespace EventManagement.Services;

/// <summary>
/// Мероприятие
/// </summary>
public class EventService : IEventService
{
  private readonly Dictionary<Guid, Event> _events = new();
  private readonly ILogger<EventService> _logger;
  /// <summary>
  /// 
  /// </summary>
  /// <param name="logger">Логгер</param>
  public EventService(ILogger<EventService> logger)
  {
    _logger = logger;
  }

  #region === CRUD ===
  /// <summary>
  /// Получить мероприятие по идентификатору
  /// </summary>
  /// <param name="id">Идентификатор мероприятия (GUID)</param>
  /// <returns>DTO мероприятия с указанным идентификатором если оно найдено</returns>  
  public EventDTO? GetById(Guid id)
  {
    _events.TryGetValue(id, out var eventItem);
    if (eventItem == null)
    {
      throw new NotFoundException(nameof(Event), id);
    }
    return EventMapper.ToDto(eventItem);
  }

  /// <summary>
  /// Создать новое мероприятие
  /// </summary>
  /// <param name="eventCreated">Данные для создания мероприятия</param>
  /// <returns>DTO созданного мероприятия</returns>
  /// <exception cref="ValidationException"></exception>
  public Task<EventDTO> CreateAsync(CreateEventDTO eventCreated)
  {
    var isExistEvent = _events.Values.Any(e =>
                       e.Title.Equals(eventCreated.Title, StringComparison.OrdinalIgnoreCase));


    if (isExistEvent)
    {
      throw new ValidationException($"Event with title '{eventCreated.Title}' already exists");
    }

    ValidateEvent(eventCreated.Title, eventCreated.StartAt, eventCreated.EndAt, eventCreated.TotalSeats);

    var eventItem = EventMapper.ToEntity(eventCreated);

    _events.TryAdd(eventItem.Id, eventItem);

    return Task.FromResult(EventMapper.ToDto(eventItem));
  }

  /// <summary>
  /// Валидация полей мероприятия
  /// </summary>
  /// <param name="title">Наименование мероприятия</param>
  /// <param name="startAt">Дата и время начала</param>
  /// <param name="endAt">Дата и время окончания</param>
  /// <param name="totalSeats">Общее количество мест</param>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="ValidationException"></exception>
  private void ValidateEvent(string title, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats)
  {
    if (string.IsNullOrEmpty(title))
    {
      throw new ArgumentException("Title is required");
    }

    if (startAt < DateTimeOffset.Now)
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

  /// <summary>
  /// Обновить существующее мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для обновления (GUID)</param>
  /// <param name="eventUpdated">Обновленные данные мероприятия</param>
  /// <returns>DTO обновленного мероприятия если оно найдено</returns>    
  public EventDTO? Update(Guid id, UpdateEventDTO eventUpdated)
  {
    if (!_events.ContainsKey(id))
    {
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

    if (eventUpdated.StartAt < DateTimeOffset.Now)
    {
      throw new ValidationException("StartAt must be more than now.");
    }

    var updatedEvent = EventMapper.ToEntity(eventUpdated, id);
    _events[id] = updatedEvent;

    return EventMapper.ToDto(updatedEvent);
  }

  /// <summary>
  /// Удалить мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для удаления (GUID)</param>
  /// <returns>true если удалось удалить</returns>
  public bool Delete(Guid id)
  {
    if (!_events.ContainsKey(id))
    {
      throw new NotFoundException(nameof(Event), id);
    }

    return _events.Remove(id);
  }
  #endregion

  #region === Фильтрация ===
  /// <summary>
  /// Получить список всех мероприятий
  /// </summary>
  /// <param name="filter">Параметры фильтра</param>
  /// <returns>Список мероприятий</returns>
  public IEnumerable<EventDTO> GetAll(EventFilterDto? filter = null)
  {

    if (filter != null && filter.From != null && filter.To != null && filter.From > filter.To)
    {
      throw new BadRequestException("'from' cannot be more than 'to'");
    }

    var query = FilteredQuery(filter);
    var events = query.OrderBy(e => e.StartAt);
    return EventMapper.ToDtoList(events);
  }

  /// <summary>
  /// Построение запроса с фильтрацией (отдельный метод для лучшей читаемости)
  /// </summary>
  /// <param name="filter">Параметры фильтра</param>
  private IQueryable<Event> FilteredQuery(EventFilterDto? filter)
  {
    var query = _events.Values.AsQueryable();

    if (filter == null)
      return query;

    query = TitleFilter(query, filter.Title);
    query = FromDateFilter(query, filter.From);
    query = ToDateFilter(query, filter.To);

    return query;
  }

  /// <summary>
  /// Фильтр по названию
  /// </summary>
  /// <param name="query">Запрос фильтрации</param>
  /// <param name="title">Именование мероприятия</param>
  private IQueryable<Event> TitleFilter(IQueryable<Event> query, string? title)
  {
    if (string.IsNullOrWhiteSpace(title))
      return query;

    return query.Where(e =>
                 e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Фильтр по дате начала
  /// </summary>
  /// <param name="query">Запрос фильтрации</param>
  /// <param name="from">С даты</param>
  private IQueryable<Event> FromDateFilter(IQueryable<Event> query, DateTimeOffset? from)
  {
    if (!from.HasValue)
      return query;

    return query.Where(e => e.StartAt >= from.Value);
  }

  /// <summary>
  /// Фильтра по дате окончания
  /// </summary>
  /// <param name="query">Запрос фильтрации</param>
  /// <param name="to">До даты</param>  
  private IQueryable<Event> ToDateFilter(IQueryable<Event> query, DateTimeOffset? to)
  {
    if (!to.HasValue)
      return query;

    return query.Where(e => e.EndAt <= to.Value);
  }
  #endregion

  #region === Пагинация ===

  /// <summary>
  /// Получить пагинированный список мероприятий с фильтрацией
  /// </summary>
  /// <param name="filter">Параметры фильтрации и пагинации</param>
  /// <returns>Пагинированный результат с мероприятиями</returns>
  public PaginatedResult<EventDTO> GetPaginated(EventFilterDto filter)
  {
    filter.Validate();

    var query = FilteredQuery(filter);

    // Общее количество записей ДО пагинации
    var totalCount = query.Count();

    // Элементы для текущей страницы
    var items = query
        .OrderBy(e => e.StartAt) // Сортировка для консистентности
        .Skip((filter.PageNumber - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToList();

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
  public bool TryReserveSeats(Guid eventId, int count = 1)
  {
    _logger.LogDebug($"Попытка забронировать {count} мест на меоприятие {eventId}");

    if (!_events.ContainsKey(eventId))
    {
      _logger.LogDebug($"Мероприятие {eventId} не найдено");
      return false;
    }

    var eventItem = _events[eventId];
    var result = eventItem.TryReserveSeats(count);

    if (result)
    {
      _logger.LogDebug($"Успешно забронировано {count} мест на мероприятие {eventId}. Отсалось доступных мест: {eventItem.AvailableSeats}");
    }
    else
    {
      _logger.LogDebug($"Не удалось забронировать {count} мест на мероприятие {eventId}. Только {eventItem.AvailableSeats} доступных мест для бронирования.");
    }

    return result;
  }
  #endregion
}