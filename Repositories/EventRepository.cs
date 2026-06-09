using Microsoft.EntityFrameworkCore;
using EventManagement.Data;
using EventManagement.DTOs;
using EventManagement.Models;

namespace EventManagement.Repositories;

public class EventRepository : IEventRepository
{
  private readonly AppDbContext _context;
  private readonly ILogger<EventRepository> _logger;

  public EventRepository(AppDbContext context, ILogger<EventRepository> logger)
  {
    _context = context;
    _logger = logger;
  }

  public async Task<Event?> GetByIdAsync(Guid id)
  {
    return await _context.Events.FindAsync(id);
  }

  public async Task<IEnumerable<Event>> GetAllAsync(EventFilterDto? filter = null)
  {
    var query = _context.Events.AsQueryable();

    if (filter != null)
    {
      if (!string.IsNullOrWhiteSpace(filter.Title))
      {
        query = query.Where(e => e.Title.ToLower() == filter.Title.ToLower());
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

  public async Task<PaginatedResult<Event>> GetPaginatedAsync(EventFilterDto filter)
  {
    filter.Validate();

    var query = _context.Events.AsQueryable();

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

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderBy(e => e.StartAt)
        .Skip((filter.PageNumber - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync();

    return new PaginatedResult<Event>(items, totalCount, filter.PageNumber, filter.PageSize);
  }

  public async Task<Event> CreateAsync(Event eventItem)
  {
    _context.Events.Add(eventItem);
    await _context.SaveChangesAsync();
    return eventItem;
  }

  public async Task<Event?> UpdateAsync(Event eventItem)
  {
    var existingEvent = await _context.Events.FindAsync(eventItem.Id);
    if (existingEvent == null)
      return null;

    existingEvent.Title = eventItem.Title;
    existingEvent.Description = eventItem.Description;
    existingEvent.StartAt = eventItem.StartAt;
    existingEvent.EndAt = eventItem.EndAt;

    await _context.SaveChangesAsync();
    return existingEvent;
  }

  public async Task<bool> DeleteAsync(Guid id)
  {
    var eventItem = await _context.Events.FindAsync(id);
    if (eventItem == null)
      return false;

    _context.Events.Remove(eventItem);
    await _context.SaveChangesAsync();
    return true;
  }

  public async Task<bool> ExistsAsync(Guid id)
  {
    return await _context.Events.AnyAsync(e => e.Id == id);
  }

  public async Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1)
  {
    var eventItem = await _context.Events.FindAsync(eventId);
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

  public async Task<bool> ReleaseSeatsAsync(Guid eventId, int count = 1)
  {
    var eventItem = await _context.Events.FindAsync(eventId);
    if (eventItem != null)
    {
      eventItem.ReleaseSeats(count);
      await _context.SaveChangesAsync();
      return true;
    }
    else
      return false;
  }

  public async Task<int> GetAvailableSeatsAsync(Guid eventId)
  {
    var eventItem = await _context.Events.FindAsync(eventId);
    return eventItem?.AvailableSeats ?? 0;
  }
}