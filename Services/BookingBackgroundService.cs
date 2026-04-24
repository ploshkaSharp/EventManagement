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
  /// 
  /// </summary>
  /// <param name="serviceProvider"></param>
  /// <param name="logger"></param>
  public BookingBackgroundService(
      IServiceProvider serviceProvider,
      ILogger<BookingBackgroundService> logger)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  protected override async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Booking background service started");

    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        await ProcessPendingBookingsAsync(cancellationToken);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while processing pending bookings");
      }

      await Task.Delay(_processingInterval, cancellationToken);
    }

    _logger.LogInformation("Booking background service stopped");
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
  {
    using var scope = _serviceProvider.CreateScope();
    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

    var pendingBookings = await bookingService.GetBookingByStatusAsync(BookingStatus.Pending);    

    if (pendingBookings.Any())
    {
      _logger.LogInformation("Found {Count} pending bookings to process", pendingBookings.Count());      
    }

    foreach (var booking in pendingBookings)
    {
      if (cancellationToken.IsCancellationRequested)
        break;

      try
      {
        _logger.LogInformation("Processing booking {BookingId} for event {EventId}",
            booking.Id, booking.EventId);

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