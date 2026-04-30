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
    var pendingBookings = await bookingService.GetBookingByStatusAsync(BookingStatus.Pending);
    var pendingList = pendingBookings.ToList();  

    if (pendingList.Any())
    {
      _logger.LogInformation("Found {Count} pending bookings to process", pendingList.Count);      
    }

    foreach (var booking in pendingList)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        _logger.LogInformation("Operation canceled. Stopping process");
        break;
      }
        

      try
      {
        _logger.LogInformation("Processing booking {BookingId} for event {EventId}", booking.Id, booking.EventId);

        // Имитация обращения к внешней системе
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        // Перевести бронь в статус Confirmed
        var success = await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Confirmed);

        if (success)
        {
          _logger.LogInformation("Booking {BookingId} successfully confirmed", booking.Id);
        }
        else
        {
          _logger.LogWarning("Failed to update booking {BookingId} status", booking.Id);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);
      }
    }
  }
}