using EventManagement.DTOs;
using EventManagement.Exceptions;
using EventManagement.Models;
using EventManagement.Data;
using Microsoft.EntityFrameworkCore;
using EventManagement.Repositories;

namespace EventManagement.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований
/// </summary>
public class BookingBackgroundService : BackgroundService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<BookingBackgroundService> _logger;
  private readonly TimeSpan _processingInterval = Constants.processingInterval;
  private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="scopeFactory"></param>
  /// <param name="logger">Логгер</param>
  public BookingBackgroundService(
      IServiceScopeFactory scopeFactory,
      ILogger<BookingBackgroundService> logger)
  {
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  /// <summary>
  /// Периодический опрос на наличие созданных бронирований
  /// </summary>
  /// <param name="stoppingToken">Токен отмены</param>
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Booking background service started");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await ProcessPendingBookingsAsync(stoppingToken);
      }
      catch (OperationCanceledException)
      {
        _logger.LogInformation("Operation canceled. Stopping process");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while processing pending bookings");
      }

      await Task.Delay(_processingInterval, stoppingToken);
    }

    _logger.LogInformation("Booking background service stopped");
  }

  /// <summary>
  /// Обработка созданных броней
  /// </summary>
  /// <param name="cancellationToken">Токен отмены</param>
  private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
  {
    List<Guid> pendingBookingIds;

    // Получить ID pending бронирований в отдельном scope
    using (var scope = _scopeFactory.CreateScope())
    {
      var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
      var pendingBookings = await bookingRepository.GetBookingByStatusAsync(BookingStatus.Pending);
      pendingBookingIds = pendingBookings.Select(b => b.Id).ToList();      
    }

    if (!pendingBookingIds.Any())
    {
      return;
    }

    _logger.LogInformation("Found {Count} pending bookings to process", pendingBookingIds.Count);

    var tasks = pendingBookingIds.Select(bookingId => ProcessBookingAsync(bookingId, cancellationToken));

    await Task.WhenAll(tasks);
  }

  /// <summary>
  /// Обработать бронирование
  /// </summary>
  /// <param name="bookingId">ИД бронирования</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns></returns>
  private async Task ProcessBookingAsync(
          Guid bookingId,
          CancellationToken cancellationToken)
  {
    try
    {
      _logger.LogInformation("Processing booking {BookingId}", bookingId);

      // Имитация обращения к внешней системе
      await Task.Delay(Constants.processingDelay, cancellationToken);

      // Захват семафора перед обновлением хранилища
      await _processingSemaphore.WaitAsync(cancellationToken);

      using var scope = _scopeFactory.CreateScope();
      var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
      var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();      
      //var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      //var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

      try
      {
        // Проверить существование бронирования и мероприятия      
        var booking = await bookingRepository.GetByIdAsync(bookingId);        

        if (booking == null)
        {
          _logger.LogWarning("Booking {BookingId} not found", bookingId);
          return;
        }

        var eventItem = await eventRepository.GetByIdAsync(booking.EventId);

        if (eventItem == null)
        {
          _logger.LogWarning("Event {EventId} not found, rejecting booking {BookingId}", booking.EventId, bookingId);
          booking.Reject();
          await bookingRepository.UpdateAsync(booking);
          return;
        }

        // Проверить, что места доступны
        if (eventItem.AvailableSeats > 0 && eventItem.StartAt >= DateTime.UtcNow)
        {
          // Подтвердить бронь 
          booking.Confirm();
          _logger.LogInformation("Booking {BookingId} successfully confirmed", bookingId);
        }
        else
        {
          booking.Reject();          
          await eventRepository.ReleaseSeatsAsync(eventItem.Id, 1);
          _logger.LogWarning("Booking {BookingId} rejected due to no available seats or event started", bookingId);
        }
        await bookingRepository.UpdateAsync(booking);
      }
      catch
      {
        throw;
      }
      finally
      {
        _processingSemaphore.Release();
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogWarning("Processing of booking {BookingId} was cancelled", bookingId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing booking {BookingId}", bookingId);

      // При любой ошибке отклонить бронь и вернуть место
      try
      {
        await DeclineBookingAsync(bookingId, cancellationToken);
      }
      catch (Exception innerEx)
      {
        _logger.LogError(innerEx, "Failed to reject booking {BookingId} after error", bookingId);
      }
    }
  }

  /// <summary>
  /// Отменить бронь с возвратом места
  /// </summary>
  /// <param name="bookingId">ИД бронирования</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns></returns>
  private async Task DeclineBookingAsync(
          Guid bookingId,
          CancellationToken cancellationToken)
  {
    using var scope = _scopeFactory.CreateScope();    
    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
    await bookingService.UpdateBookingStatusAsync(bookingId, BookingStatus.Rejected);
  }
}