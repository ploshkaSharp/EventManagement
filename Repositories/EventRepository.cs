using Microsoft.EntityFrameworkCore;
using EventManagement.Data;
using EventManagement.DTOs;
using EventManagement.Models;

namespace EventManagement.Repositories;

/// <summary>
/// Репозиторий мероприятий
/// </summary>
public class EventRepository : IEventRepository
{
  private readonly AppDbContext _context;
  private readonly ILogger<EventRepository> _logger;

  /// <summary>
  /// 
  /// </summary>
  /// <param name="context">Контекст БД</param>
  /// <param name="logger">Логгер</param>
  public EventRepository(AppDbContext context, ILogger<EventRepository> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <summary>
  /// Получить мероприятие по ИД
  /// </summary>
  /// <param name="id">ИД мероприятия</param>
  /// <returns></returns>
  public async Task<Event?> GetByIdAsync(Guid id)
  {
    return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
  }

  /// <summary>
  /// Получить мероприятия по фильтру
  /// </summary>
  /// <param name="filter">Фильтр</param>
  /// <returns></returns>
  public async Task<IEnumerable<Event>> GetAllAsync(EventFilterDto? filter = null)
  {
    var query = _context.Events.AsQueryable();

    if (filter != null)
    {
      if (!string.IsNullOrWhiteSpace(filter.Title))
      {
        query = query.Where(e => e.Title.ToLower().Contains(filter.Title.ToLower()));
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

    return await query.OrderBy(e => e.StartAt).ToListAsync();
  }

  /// <summary>
  /// Получить пагинированный результат мероприятий
  /// </summary>
  /// <param name="filter">Фильтр</param>
  /// <returns></returns>
  public async Task<PaginatedResult<Event>> GetPaginatedAsync(EventFilterDto filter)
  {
    filter.Validate();

    var query = _context.Events.AsQueryable();

    if (!string.IsNullOrWhiteSpace(filter.Title))
    {
      query = query.Where(e => e.Title.ToLower().Contains(filter.Title.ToLower()));
    }

    if (filter.From.HasValue)
    {
      query = query.Where(e => e.StartAt >= filter.From.Value);
    }

    if (filter.To.HasValue)
    {
      query = query.Where(e => e.EndAt <= filter.To.Value);
    }

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderBy(e => e.StartAt)
        .Skip((filter.PageNumber - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync();

    return new PaginatedResult<Event>(items, totalCount, filter.PageNumber, filter.PageSize);
  }

  /// <summary>
  /// Создать мероприятие
  /// </summary>
  /// <param name="eventItem">Мероприятие</param>
  /// <returns></returns>
  public async Task<Event> CreateAsync(Event eventItem)
  {
    _context.Events.Add(eventItem);
    await _context.SaveChangesAsync();
    return eventItem;
  }

  /// <summary>
  /// Обновить информацию о мероприятии
  /// </summary>
  /// <param name="eventItem">Мероприятие</param>
  /// <returns></returns>
  public async Task<Event?> UpdateAsync(Event eventItem)
  {
    var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventItem.Id);
    if (existingEvent == null)
      return null;

    existingEvent.Title = eventItem.Title;
    existingEvent.Description = eventItem.Description;
    existingEvent.StartAt = eventItem.StartAt;
    existingEvent.EndAt = eventItem.EndAt;

    await _context.SaveChangesAsync();
    return existingEvent;
  }

  /// <summary>
  /// Удалить мероприятие
  /// </summary>
  /// <param name="id">ИД мероприятия</param>
  /// <returns></returns>
  public async Task<bool> DeleteAsync(Guid id)
  {
    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
    if (eventItem == null)
      return false;

    _context.Events.Remove(eventItem);
    await _context.SaveChangesAsync();
    return true;
  }

  /// <summary>
  /// Забронировать место
  /// </summary>
  /// <param name="eventId">ИД мероприятия</param>
  /// <param name="count">Количесвто мест</param>
  /// <returns></returns>
  public async Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1)
  {
    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
    if (eventItem == null)
      return false;

    if (eventItem.AvailableSeats >= count)
    {
      eventItem.AvailableSeats -= count;
      await _context.SaveChangesAsync();
      return true;
    }

    return false;
  }

/// <summary>
/// Освободить места
/// </summary>
/// <param name="eventId">ИД мероприятия</param>
/// <param name="count">Количество мест</param>
/// <returns></returns>
  public async Task<bool> ReleaseSeatsAsync(Guid eventId, int count = 1)
  {
    var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
    if (eventItem != null)
    {
      eventItem.ReleaseSeats(count);
      await _context.SaveChangesAsync();
      return true;
    }
    else
      return false;
  }
}