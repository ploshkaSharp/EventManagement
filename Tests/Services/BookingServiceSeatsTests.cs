using Xunit;
using EventManagement.Services;
using EventManagement.DTOs;
using EventManagement.Exceptions;
using EventManagement.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagement.Tests.Services;

public class BookingServiceSeatsTests
{
  private readonly IEventService _eventService;
  private readonly IBookingService _bookingService;

  public BookingServiceSeatsTests()
  {
    _eventService = new EventService(NullLogger<EventService>.Instance);
    _bookingService = new BookingService(_eventService, NullLogger<BookingService>.Instance);
  }

  #region Успешные сценарии

  [Fact]
  public async Task CreateBooking_ShouldDecreaseAvailableSeats()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 10
    };

    var createdEvent = _eventService.Create(createEventDto);

    // Act
    var booking = await _bookingService.CreateBookingAsync(createdEvent.Id);

    // Assert
    var updatedEvent = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(updatedEvent);
    Assert.Equal(9, updatedEvent.AvailableSeats);
    Assert.Equal(10, updatedEvent.TotalSeats);
  }

  [Fact]
  public async Task CreateMultipleBookings_UntilLimit_ShouldAllSucceed()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 5
    };

    var createdEvent = _eventService.Create(createEventDto);
    var bookings = new List<BookingDTO>();

    // Act
    for (int i = 0; i < 5; i++)
    {
      var booking = await _bookingService.CreateBookingAsync(createdEvent.Id);
      bookings.Add(booking);
    }

    // Assert
    Assert.Equal(5, bookings.Count);
    var uniqueIds = bookings.Select(b => b.Id).Distinct();
    Assert.Equal(5, uniqueIds.Count());

    var updatedEvent = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(updatedEvent);
    Assert.Equal(0, updatedEvent.AvailableSeats);
  }

  [Fact]
  public async Task CreateBooking_WhenNoSeatsLeft_ShouldThrowNoAvailableSeatsException()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 1
    };

    var createdEvent = _eventService.Create(createEventDto);
    await _bookingService.CreateBookingAsync(createdEvent.Id);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
        _bookingService.CreateBookingAsync(createdEvent.Id));

    Assert.Contains("No available seats", exception.Message);
  }

  #endregion

  #region Неуспешные сценарии

  [Fact]
  public async Task CreateBooking_ForNonExistentEvent_ShouldThrowNotFoundException()
  {
    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
        _bookingService.CreateBookingAsync(Guid.NewGuid()));

    Assert.Contains("not found", exception.Message);
  }

  [Fact]
  public void CreateBooking_WhenNoSeats_ShouldThrowNoAvailableSeatsException()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 0
    };

    // Act & Assert
    var exception = Assert.Throws<ValidationException>(() =>
        _eventService.Create(createEventDto));

    Assert.Contains("TotalSeats must be greater than 0", exception.Message);
  }

  #endregion

  #region Тесты на смену статуса

  [Fact]
  public async Task ConfirmBooking_ShouldSetStatusToConfirmedAndSetProcessedAt()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 10
    };

    var createdEvent = _eventService.Create(createEventDto);
    var booking = await _bookingService.CreateBookingAsync(createdEvent.Id);

    // Act
    var success = await _bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Confirmed);

    // Assert
    Assert.True(success);
    var updatedBooking = await _bookingService.GetBookingByIdAsync(booking.Id);
    Assert.NotNull(updatedBooking);
    Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
    Assert.NotNull(updatedBooking.ProcessedAt);
  }

  [Fact]
  public async Task RejectBooking_ShouldReleaseSeats()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 5
    };

    var createdEvent = _eventService.Create(createEventDto);
    var booking = await _bookingService.CreateBookingAsync(createdEvent.Id);

    var beforeReject = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(beforeReject);
    Assert.Equal(4, beforeReject.AvailableSeats);

    // Act
    var successReject = await _bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Rejected);
    var successRelease = _eventService.ReleaseSeats(booking.EventId, 1);

    // Assert
    Assert.True(successReject);
    Assert.True(successRelease);

    var afterReject = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(afterReject);
    Assert.Equal(5, afterReject.AvailableSeats);
    var updatedBooking = await _bookingService.GetBookingByIdAsync(booking.Id);
    Assert.NotNull(updatedBooking);
    Assert.Equal(BookingStatus.Rejected, updatedBooking.Status);
    Assert.NotNull(updatedBooking.ProcessedAt);        

    // Можно создать новую бронь
    var newBooking = await _bookingService.CreateBookingAsync(createdEvent.Id);
    Assert.NotNull(newBooking);    
  }

  #endregion

  #region Тесты на конкурентность
  [Fact]
  public async Task ConcurrentBookings_ShouldProtectAgainstOverbooking()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Concurrency Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 5
    };

    var createdEvent = _eventService.Create(createEventDto);

    // Act 
    // 20 конкурентных запросов    
    var tasks = new List<Task<BookingDTO>>();
    for (int i = 0; i < 20; i++)
    {
      tasks.Add(Task.Run(async () =>
          await _bookingService.CreateBookingAsync(createdEvent.Id)));
    }

    // Assert
    // Выполнились успешно
    var results = await Task.WhenAll(tasks.Select(tsk => tsk.ContinueWith(t => t.IsCompletedSuccessfully)));
    Assert.Equal(5, results.Where(r => r != false).Count());

    // Проверить исключения    
    var exceptions = new List<Exception>();
    foreach (var task in tasks)
    {
      try
      {
        await task;
      }
      catch (Exception ex)
      {
        exceptions.Add(ex);
      }
    }

    var noSeatExceptions = exceptions.Count(e => e is NoAvailableSeatsException);
    Assert.Equal(15, noSeatExceptions);

    // Проверить конечное состояние
    var finalEvent = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(finalEvent);
    Assert.Equal(0, finalEvent.AvailableSeats);
  }

  [Fact]
  public async Task ConcurrentBookings_ShouldGenerateUniqueIds()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Unique Id Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 10
    };

    var createdEvent = _eventService.Create(createEventDto);

    // Act
    var tasks = new List<Task<BookingDTO>>();
    for (int i = 0; i < 10; i++)
    {
      tasks.Add(Task.Run(async () =>
          await _bookingService.CreateBookingAsync(createdEvent.Id)));
    }

    var results = await Task.WhenAll(tasks);

    // Assert
    var bookingIds = results.Select(b => b.Id).ToList();
    var uniqueIds = bookingIds.Distinct().ToList();

    Assert.Equal(10, bookingIds.Count);
    Assert.Equal(10, uniqueIds.Count);
  }

  [Fact]
  public void CreateEvent_WithInvalidTotalSeats_ShouldThrowValidationException()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Invalid Seats Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = -5
    };

    // Act & Assert
    var exception = Assert.Throws<ValidationException>(() =>
        _eventService.Create(createEventDto));

    Assert.Contains("TotalSeats must be greater than 0", exception.Message);
  }

  [Fact]
  public async Task TryReserveSeats_WhenNotEnoughSeats_ShouldReturnFalse()
  {
    // Arrange    
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 3
    };

    var createdEvent = _eventService.Create(createEventDto);

    // Act
    var result1 = _eventService.TryReserveSeats(createdEvent.Id, 2);
    var result2 = _eventService.TryReserveSeats(createdEvent.Id, 2);

    // Assert
    Assert.True(result1);
    Assert.False(result2);

    var finalEvent = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(finalEvent);
    Assert.Equal(1, finalEvent.AvailableSeats);
  }

  [Fact]
  public void ReleaseSeats_ShouldIncreaseAvailableSeats()
  {
    // Arrange
    var createEventDto = new CreateEventDTO
    {
      Title = "Test Event",
      StartAt = DateTime.Now.AddDays(30),
      EndAt = DateTime.Now.AddDays(30).AddHours(4),
      TotalSeats = 10
    };

    var createdEvent = _eventService.Create(createEventDto);
    _eventService.TryReserveSeats(createdEvent.Id, 3);

    var beforeRelease = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(beforeRelease);
    Assert.Equal(7, beforeRelease.AvailableSeats);

    // Act
    _eventService.ReleaseSeats(createdEvent.Id, 2);

    // Assert
    var afterRelease = _eventService.GetById(createdEvent.Id);
    Assert.NotNull(afterRelease);
    Assert.Equal(9, afterRelease.AvailableSeats);
  }  
  #endregion  
}