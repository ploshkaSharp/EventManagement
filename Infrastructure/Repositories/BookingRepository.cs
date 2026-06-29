using Microsoft.EntityFrameworkCore;
using EventManagement.Infrastructure.Data;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Enums;
using EventManagement.Application.Ports;
using Microsoft.Extensions.Logging;

namespace EventManagement.Infrastructure.Repositories;

/// <summary>
/// Репозиторий бронирований
/// </summary>
public class BookingRepository : IBookingRepository
{
  private readonly AppDbContext _context;
  private readonly ILogger<BookingRepository> _logger;

  /// <summary>
  /// 
  /// </summary>
  /// <param name="context">Контекст БД</param>
  /// <param name="logger">Логгер</param>
  public BookingRepository(AppDbContext context, ILogger<BookingRepository> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <summary>
  /// Получить бронь по ИД
  /// </summary>
  /// <param name="id">ИД брони</param>
  /// <returns></returns>
  public async Task<Booking?> GetByIdAsync(Guid id)
  {
    return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
  }

  /// <summary>
  /// Получить брони по ИД мероприятия
  /// </summary>
  /// <param name="eventId">ИД мероприятия</param>
  /// <returns></returns>
  public async Task<IEnumerable<Booking>> GetByEventIdAsync(Guid eventId)
  {
    return await _context.Bookings
        .Where(b => b.EventId == eventId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
  }

  /// <summary>
  /// Получить брони по стаусу
  /// </summary>
  /// <param name="status">Статус бронирования</param>
  /// <returns></returns>
  public async Task<IEnumerable<Booking>> GetBookingByStatusAsync(BookingStatus status)
  {
    return await _context.Bookings
            .Where(b => b.Status == status)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();
  }

  /// <summary>
  /// Создать бронь
  /// </summary>
  /// <param name="booking">Бронь</param>
  /// <returns></returns>
  public async Task<Booking> CreateAsync(Booking booking)
  {
    _context.Bookings.Add(booking);
    await _context.SaveChangesAsync();
    return booking;
  }

  /// <summary>
  /// Обновить информацию о бронировании
  /// </summary>
  /// <param name="booking">Бронь</param>
  /// <returns></returns>
  public async Task<Booking?> UpdateAsync(Booking booking)
  {
    var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
    if (existingBooking == null)
      return null;

    existingBooking.Status = booking.Status;
    existingBooking.ProcessedAt = booking.ProcessedAt;

    await _context.SaveChangesAsync();
    return existingBooking;
  }

  /// <summary>
  /// Удалить бронь по ИД
  /// </summary>
  /// <param name="id">ИД брони</param>
  /// <returns></returns>
  public async Task<bool> DeleteAsync(Guid id)
  {
    var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    if (booking == null)
      return false;

    _context.Bookings.Remove(booking);
    await _context.SaveChangesAsync();
    return true;
  }
}