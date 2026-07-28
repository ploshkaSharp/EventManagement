using EventManagement.Bookings.Application.DTOs;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Domain.Enums;
using EventManagement.Bookings.Domain.Entities;
using EventManagement.Bookings.Domain.Exceptions;
using EventManagement.Bookings.Application.Mappers;
using EventManagement.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace EventManagement.Bookings.Application.Services;

/// <summary>
/// Сервис для управления бронированиями
/// </summary>
public class BookingService : IBookingService
{
  private readonly IBookingRepository _bookingRepository;
  private readonly IUserService _userService;
  private readonly IEventPublisher _eventPublisher;  
  private readonly int _maxActiveBookings = 10;
  private readonly ILogger<BookingService> _logger;
  private static readonly SemaphoreSlim _bookingLock = new(1, 1); // Блокировка для критической секции

  /// <summary>
  /// 
  /// </summary>
  /// <param name="bookingRepository"></param>
  /// <param name="eventRepository"></param>
  /// <param name="userRepository"></param>
  /// <param name="logger"></param>
  public BookingService(IBookingRepository bookingRepository, IEventPublisher eventPublisher, IUserService userService, ILogger<BookingService> logger)
  {
    _bookingRepository = bookingRepository;
    _eventPublisher = eventPublisher;
    _userService = userService;
    _logger = logger;
  }

  /// <summary>
  /// Создать бронь
  /// </summary>
  /// <param name="eventId">ИД мероприятия</param>
  /// <param name="userId">ИД пользователя</param>
  /// <returns></returns>
  /// <exception cref="NotFoundException"></exception>
  /// <exception cref="BadRequestException"></exception>
  public async Task<BookingDTO> CreateBookingAsync(Guid eventId, Guid userId)
  {
    _logger.LogInformation("Attempting to create booking for event {EventId}", eventId);
        await _bookingLock.WaitAsync();
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                throw new NotFoundException(nameof(user), userId);
            
            var activeBookings = await _bookingRepository.CountActiveBookingsAsync(userId);
            if (activeBookings >= _maxActiveBookings)
                throw new BookingLimitExceededException(_maxActiveBookings);
            
            var booking = new Booking(eventId, userId);
            var created = await _bookingRepository.CreateAsync(booking);
            
            // Публикуем событие подтверждения брони
            await _eventPublisher.PublishBookingConfirmedAsync(
                new BookingConfirmedEvent(                
                    created.Id,
                    eventId,
                    userId,
                    1,
                    DateTime.UtcNow
                ));
            
            return BookingMapper.ToDto(created);
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
  public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin)
  {
    _logger.LogDebug("Retrieving booking {BookingId}", bookingId);

    var booking = await _bookingRepository.GetByIdAsync(bookingId);

    if (booking == null)
    {
      throw new NotFoundException(nameof(Booking), bookingId);
    }

    if (!isAdmin && booking.UserId != userId)
    {
      throw new UnAuthorizedOperationException("view booking", "User can only view their own bookings");
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

  /// <summary>
  /// Отменить бронирование
  /// </summary>
  /// <param name="bookingId">ИД брони/param>
  /// <param name="userId">ИД пользователя</param>
  /// <param name="isAdmin">Роль</param>
  /// <returns></returns>
  /// <exception cref="NotFoundException"></exception>
  /// <exception cref="UnAuthorizedOperationException"></exception>
  /// <exception cref="ValidationException"></exception>
  public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin)
  {
    var booking = await _bookingRepository.GetByIdAsync(bookingId);
    if (booking == null)
      throw new NotFoundException(nameof(Booking), bookingId);

    if (!isAdmin && booking.UserId != userId)
      throw new UnAuthorizedOperationException("cancel booking", "User can only cancel their own bookings");

    if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
      throw new ValidationException($"Cannot cancel booking with status {booking.Status}");  

    booking.Cancel();
    
    var updated = await _bookingRepository.UpdateAsync(booking);    

    return updated != null;
  }
}