using System.Collections.Concurrent;
using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Exceptions;
using EventManagement.Mappers;

namespace EventManagement.Services;

/// <summary>
/// Сервис для управления бронированиями
/// </summary>
public class BookingService : IBookingService
{
  private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
  private readonly IEventService _eventService;
  private readonly ILogger<BookingService> _logger;

  /// <summary>
  /// 
  /// </summary>
  /// <param name="eventService"></param>
  /// <param name="logger"></param>
  public BookingService(IEventService eventService, ILogger<BookingService> logger)
  {
    _eventService = eventService;
    _logger = logger;
  }

  /// <summary>
  /// Создать бронь
  /// </summary>
  /// <param name="eventId">ИД мероприятия</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns></returns>
  /// <exception cref="NotFoundException"></exception>
  /// <exception cref="BadRequestException"></exception>
  public async Task<BookingDTO> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    // Проверить существование мероприятия
    var eventItem = await Task.Run(() => _eventService.GetById(eventId));

    if (eventItem == null)
    {
      throw new NotFoundException(nameof(Event), eventId);
    }

    if (eventItem.StartAt < DateTimeOffset.Now)
    {
      throw new BadRequestException("Can not book an event that has already started");
    }

    var booking = new Booking
    {
      Id = Guid.NewGuid(),
      EventId = eventId,
      Status = BookingStatus.Pending,
      CreatedAt = DateTimeOffset.Now
    };

    cancellationToken.ThrowIfCancellationRequested();
    // Добавить бронь
    var added = await Task.Run(() => _bookings.TryAdd(booking.Id, booking));

    if (!added)
    {
      throw new BadRequestException("Failed to create booking");
    }

    return BookingMapper.ToDto(booking);
  }

  /// <summary>
  /// Найти бронь по ИД
  /// </summary>
  /// <param name="bookingId">Идентификатор брони</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns>Информация о брони</returns>
  /// <exception cref="NotFoundException"></exception>
  public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var booking = await Task.Run(() =>
    {
      _bookings.TryGetValue(bookingId, out var book);
      return book;
    });

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
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns>Список инфо о брони</returns>
  public async Task<IEnumerable<BookingDTO>> GetBookingByStatusAsync(BookingStatus status, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var pendingBookings = await Task.Run(() =>
        _bookings.Values
            .Where(b => b.Status == status)
            .OrderBy(b => b.CreatedAt)
            .Select(BookingMapper.ToDto)
            );

    return pendingBookings;
  }

  /// <summary>
  /// Обновить статус брони
  /// </summary>
  /// <param name="bookingId">ИД брони</param>
  /// <param name="status">Новый статус</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns>true если удалось обновить, fasle если не удалось</returns>
  public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var result = await Task.Run(() =>
    {
      if (!_bookings.TryGetValue(bookingId, out var booking))
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
      booking.ProcessedAt = DateTimeOffset.Now;
      _bookings[bookingId] = booking;

      return true;
    });

    return result;
  }
}