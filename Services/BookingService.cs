using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Exceptions;
using EventManagement.Mappers;
using EventManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Services;

/// <summary>
/// Сервис для управления бронированиями
/// </summary>
public class BookingService : IBookingService
{
  private readonly AppDbContext _context;
  private readonly ILogger<BookingService> _logger;
  private static readonly SemaphoreSlim _bookingLock = new(1, 1); // Блокировка для критической секции

  /// <summary>
  /// 
  /// </summary>
  /// <param name="context"></param>
  /// <param name="logger"></param>
  public BookingService(AppDbContext context, ILogger<BookingService> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <summary>
  /// Создать бронь
  /// </summary>
  /// <param name="eventId">ИД мероприятия</param>
  /// <returns></returns>
  /// <exception cref="NotFoundException"></exception>
  /// <exception cref="BadRequestException"></exception>
  public async Task<BookingDTO> CreateBookingAsync(Guid eventId)
  { 
    _logger.LogInformation("Attempting to create booking for event {EventId}", eventId);
    await _bookingLock.WaitAsync();
    try
    {
      // Проверить существование мероприятия
      var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);

      if (eventItem == null)
      {
        throw new NotFoundException(nameof(Event), eventId);
      }

      if (eventItem.StartAt < DateTime.UtcNow)
      {
        throw new BadRequestException("Can not book an event that has already started");
      }

      // Забронировать место
      if (!eventItem.TryReserveSeats(1))
      {
        _logger.LogWarning($"No available seats for event {eventId}");
        throw new NoAvailableSeatsException("No available seats for this event");
      }

      var booking = new Booking(eventId){};

      // Добавить бронь      
      _context.Bookings.Add(booking);    
      await _context.SaveChangesAsync();

      return BookingMapper.ToDto(booking);
    }
    finally
    {
      _bookingLock.Release();
      
    }
  }

  /// <summary>
  /// Найти бронь по ИД
  /// </summary>
  /// <param name="bookingId">Идентификатор брони</param>
  /// <returns>Информация о брони</returns>
  /// <exception cref="NotFoundException"></exception>
  public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId)
  {
    _logger.LogDebug("Retrieving booking {BookingId}", bookingId);

    var booking = await _context.Bookings.FirstOrDefaultAsync(e => e.Id == bookingId);    

    if (booking == null)
    {
      throw new NotFoundException(nameof(Booking), bookingId);
    }

    return BookingMapper.ToDto(booking);
  }

  /// <summary>
  /// Получить список бронирований по статусу
  /// </summary>
  /// <param name="status">Статус бронирования</param>
  /// <returns>Список инфо о брони</returns>
  public async Task<IEnumerable<BookingDTO>> GetBookingByStatusAsync(BookingStatus status)
  {
    _logger.LogDebug("Get booking by status {Status}", status);

    var pendingBookings = await _context.Bookings
            .Where(b => b.Status == status)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();            

    return pendingBookings.Select(BookingMapper.ToDto);
  }

  /// <summary>
  /// Обновить статус брони
  /// </summary>
  /// <param name="bookingId">ИД брони</param>
  /// <param name="status">Новый статус</param>
  /// <returns>true если удалось обновить, fasle если не удалось</returns>
  public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status)
  {
    _logger.LogDebug("Attempting to update booking {BookingId} status to {Status}", bookingId, status);

    var booking = await _context.Bookings.FirstOrDefaultAsync(e => e.Id == bookingId);
    if (booking == null)
    {
      _logger.LogWarning($"Not found booking with id='{bookingId.ToString()}'");
      return false;
    }

    // Можно обновить статус только из Pending
    if (booking.Status != BookingStatus.Pending)
    {
      _logger.LogWarning($"Can not update status. Status of booking id='{bookingId.ToString()}' is not Pending ('{booking.Status.ToString()}')");
      return false;
    }

    booking.Status = status;
    booking.ProcessedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return true;
  }
}