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

  /// <summary>
  /// 
  /// </summary>
  /// <param name="eventService"></param>
  public BookingService(IEventService eventService)
  {
    _eventService = eventService;
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
    // Проверить существование мероприятия
    var eventItem = await Task.Run(() => _eventService.GetById(eventId));

    if (eventItem == null)
    {
      throw new NotFoundException($"Event '{nameof(Event)}' not found", eventId);
    }

    if (eventItem.StartAt < DateTime.UtcNow)
    {
      throw new BadRequestException("Can not book an event that has already started");
    }

    var booking = new Booking
    {
      Id = Guid.NewGuid(),
      EventId = eventId,
      Status = BookingStatus.Pending,
      CreatedAt = DateTime.UtcNow,
      ProcessedAt = null
    };

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
  /// <returns>Информация о брони</returns>
  public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId)
  {
    var booking = await Task.Run(() =>
    {
      _bookings.TryGetValue(bookingId, out var book);
      return book;
    });

    if (booking == null)
    {
      throw new NotFoundException("Booking not found", bookingId);
    }

    return BookingMapper.ToDto(booking);
  }
}