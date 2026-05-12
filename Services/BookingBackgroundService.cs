using EventManagement.DTOs;
using EventManagement.Models;

namespace EventManagement.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований
/// </summary>
public class BookingBackgroundService : BackgroundService
{
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<BookingBackgroundService> _logger;
  private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);
  private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="serviceProvider">Провайдер сервисов</param>
  /// <param name="logger">Логгер</param>
  public BookingBackgroundService(
      IServiceProvider serviceProvider,
      ILogger<BookingBackgroundService> logger)
  {
    _serviceProvider = serviceProvider;
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
    using var scope = _serviceProvider.CreateScope();
    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
    var pendingBookings = await bookingService.GetBookingByStatusAsync(BookingStatus.Pending);
    var pendingList = pendingBookings.ToList();

    if (pendingList.Any())
    {
      _logger.LogInformation("Found {Count} pending bookings to process", pendingList.Count);
    }

    var tasks = pendingList.Select(booking =>
        ProcessBookingAsync(booking, bookingService, eventService, cancellationToken));

    await Task.WhenAll(tasks);
  }

  private async Task ProcessBookingAsync(
          BookingDTO booking,
          IBookingService bookingService,
          IEventService eventService,
          CancellationToken cancellationToken)
  {
    try
    {
      _logger.LogInformation("Processing booking {BookingId} for event {EventId}", booking.Id, booking.EventId);

      // Имитация обращения к внешней системе
      await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

      // Захват семафора перед обновлением хранилища
      await _processingSemaphore.WaitAsync(cancellationToken);

      try
      {
        // Проверить существование события
        var eventItem = eventService.GetById(booking.EventId);

        if (eventItem == null)
        {
          // Если мероприятие удалено отклонить бронь
          _logger.LogWarning("Event {EventId} not found, rejecting booking {BookingId}",
              booking.EventId, booking.Id);

          await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Rejected);

          return;
        }

        // Подтвердить бронь
        var success = await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Confirmed);

        if (success)
        {
          _logger.LogInformation("Booking {BookingId} successfully confirmed", booking.Id);
        }
        else
        {
          _logger.LogWarning("Failed to update booking {BookingId} status", booking.Id);

          // Если не удалось подтвердить, вернуть место
          if (!eventService.ReleaseSeats(booking.EventId, 1))
            _logger.LogError("Failed cancel booking 1 seat for the event {EventId}", booking.EventId);
        }
      }
      finally
      {
        _processingSemaphore.Release();
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogWarning("Processing of booking {BookingId} was cancelled", booking.Id);

      // Отменить бронь и вернуть место
      await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Rejected);
      if (!eventService.ReleaseSeats(booking.EventId, 1))
        _logger.LogError("Failed cancel booking 1 seat for the event {EventId}", booking.EventId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);

      // При любой ошибке отклонить бронь и вернуть место
      try
      {
        await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Rejected);
        if (!eventService.ReleaseSeats(booking.EventId, 1))
          _logger.LogError("Failed cancel booking 1 seat for the event {EventId}", booking.EventId);
      }
      catch (Exception innerEx)
      {
        _logger.LogError(innerEx, "Failed to reject booking {BookingId} after error", booking.Id);
      }
    }
  }
}