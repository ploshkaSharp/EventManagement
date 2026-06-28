using EventManagement.Application.DTOs;
using EventManagement.Application.Ports;
using EventManagement.Domain.Enums;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Exceptions;
using EventManagement.Application.Mappers;

namespace EventManagement.Application.Services;

/// <summary>
/// Сервис для управления бронированиями
/// </summary>
public class BookingService : IBookingService
{
  private readonly IBookingRepository _bookingRepository;
  private readonly IEventRepository _eventRepository;
  private readonly ILogger<BookingService> _logger;
  private static readonly SemaphoreSlim _bookingLock = new(1, 1); // Блокировка для критической секции

  /// <summary>
  /// 
  /// </summary>
  /// <param name="bookingRepository"></param>
  /// <param name="eventRepository"></param>
  /// <param name="logger"></param>
  public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingService> logger)
  {    
    _bookingRepository = bookingRepository;
    _eventRepository = eventRepository;
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
      var eventItem = await _eventRepository.GetByIdAsync(eventId);

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
      var createdBooking = await _bookingRepository.CreateAsync(booking);

      return BookingMapper.ToDto(createdBooking);
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
    
    var booking = await _bookingRepository.GetByIdAsync(bookingId);

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
    
    var pendingBookings = await _bookingRepository.GetBookingByStatusAsync(status);   

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
    
    var booking = await _bookingRepository.GetByIdAsync(bookingId);

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
    
    var updatedBooking = await _bookingRepository.UpdateAsync(booking);
    return updatedBooking != null;    
  }
}