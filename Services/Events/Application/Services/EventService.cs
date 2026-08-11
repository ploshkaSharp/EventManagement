using EventManagement.Events.Application.DTOs;
using EventManagement.Events.Application.Mappers;
using EventManagement.Events.Application.Ports;
using EventManagement.Events.Application.Constants;
using EventManagement.Events.Domain.Entities;
using EventManagement.Events.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventManagement.Events.Application.Services;

/// <summary>
/// Мероприятие
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly IOptions<CacheSettings> _cacheSettings;    
    private readonly ILogger<EventService> _logger;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventRepository">Репозиторий мероприятий</param>
    /// <param name="logger">Логгер</param>
    public EventService(IEventRepository eventRepository, ILogger<EventService> logger, ICacheService cacheService, IOptions<CacheSettings> cacheSettings)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings;        
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

        var cacheKey = CacheKeys.EventKey(id);
        try
        {
            var cached = await _cacheService.GetAsync<EventDTO>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for event {EventId}", id);
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for event {EventId}, fallback to DB", id);
        }

        _logger.LogDebug("Cache miss for event {EventId}, fetching from DB", id);        

        var eventItem = await _eventRepository.GetByIdAsync(id);

        if (eventItem == null)
        {
            _logger.LogDebug("Event {EventId} not found", id);
            throw new NotFoundException(nameof(Event), id);
        }
        
        var dto = EventMapper.ToDto(eventItem);
        // Сохранить в кеш с TTL
        var ttl = TimeSpan.FromSeconds(_cacheSettings.Value.Event);
        await _cacheService.SetAsync(cacheKey, dto, ttl);

        _logger.LogDebug("Successfully retrieved event {EventId}", id);
        return dto;
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
        // Инвалидация топ-10 
        await InvalidateTop10CacheAsync();        
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

        if (result != null)
        {
            await InvalidateEventCacheAsync(id);
            await InvalidateTop10CacheAsync();
        }        

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

        var deleted = await _eventRepository.DeleteAsync(id);

        if (deleted)
        {
            await InvalidateEventCacheAsync(id);
            await InvalidateTop10CacheAsync();
        }

        return deleted;
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
    
    /// <summary>
    /// Топ-10 самых популярных событий 
    /// </summary>
    public async Task<IEnumerable<EventDTO>> GetTop10Async()
    {
        const string cacheKey = CacheKeys.Top10Events;
        try
        {
            var cached = await _cacheService.GetAsync<IEnumerable<EventDTO>>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for top10 events");
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for top10, fallback to DB");
        }

        _logger.LogDebug("Cache miss for top10, fetching from DB");
        var events = await _eventRepository.GetTop10ByPopularityAsync();
        var dtos = events.Select(e => EventMapper.ToDto(e)).ToList();
        var ttl = TimeSpan.FromSeconds(_cacheSettings.Value.Top10);
        await _cacheService.SetAsync(cacheKey, dtos, ttl);
        return dtos;
    }    

    /// <summary>
    /// Метод для обновления кеша после изменения AvailableSeats (вызывается из Kafka обработчика)
    /// </summary> 
    public async Task UpdateEventCacheAsync(Guid eventId)
    {
        // Просто инвалидируем и прогреем при следующем запросе
        await InvalidateEventCacheAsync(eventId);
        // Топ-10 тоже инвалидируем, так как популярность изменилась
        await InvalidateTop10CacheAsync();
    }    

    private async Task InvalidateEventCacheAsync(Guid eventId)
    {
        var key = CacheKeys.EventKey(eventId);
        await _cacheService.RemoveAsync(key);
        _logger.LogDebug("Invalidated cache for event {EventId}", eventId);
    }    

    private async Task InvalidateTop10CacheAsync()
    {
        await _cacheService.RemoveAsync(CacheKeys.Top10Events);
        _logger.LogDebug("Invalidated top10 cache");
    }    
}