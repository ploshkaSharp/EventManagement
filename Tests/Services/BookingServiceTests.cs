using Xunit;
using EventManagement.Services;
using EventManagement.DTOs;
using EventManagement.Exceptions;
using EventManagement.Models;
using Microsoft.Extensions.Logging.Abstractions;


namespace EventManagement.Tests.Services;

public class BookingServiceTests
{
  #region Успешные сценарии 

  /// <summary>
  /// Создать бронь существующего мероприятия
  /// </summary>
  [Fact]
  public async Task CreateBooking_ForExistingEvent_ShouldReturnBookingWithPendingStatus()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);

    // Act
    var booking = await bookingService.CreateBookingAsync(createdEvent.Id);

    // Assert
    Assert.NotNull(booking);
    Assert.NotEqual(Guid.Empty, booking.Id);
    Assert.Equal(createdEvent.Id, booking.EventId);
    Assert.Equal(BookingStatus.Pending, booking.Status);
    Assert.Null(booking.ProcessedAt);
  }

  /// <summary>
  /// Создать несколько броней на одно мероприятие
  /// </summary>
  [Fact]
  public async Task CreateBooking_MultipleBookingsForSameEvent_ShouldCreateUniqueIds()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);

    // Act
    var booking1 = await bookingService.CreateBookingAsync(createdEvent.Id);
    var booking2 = await bookingService.CreateBookingAsync(createdEvent.Id);
    var booking3 = await bookingService.CreateBookingAsync(createdEvent.Id);

    // Assert
    Assert.NotNull(booking1);
    Assert.NotNull(booking2);
    Assert.NotNull(booking3);

    Assert.NotEqual(booking1.Id, booking2.Id);
    Assert.NotEqual(booking1.Id, booking3.Id);
    Assert.NotEqual(booking2.Id, booking3.Id);

    Assert.All(new[] { booking1, booking2, booking3 }, b =>
        Assert.Equal(createdEvent.Id, b.EventId));
    Assert.All(new[] { booking1, booking2, booking3 }, b =>
        Assert.Equal(BookingStatus.Pending, b.Status));
  }

  /// <summary>
  /// Получить бронь по ИД
  /// </summary>
  [Fact]
  public async Task GetBookingById_WithExistingBooking_ShouldReturnCorrectBooking()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие и бронь
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);
    var createdBooking = await bookingService.CreateBookingAsync(createdEvent.Id);

    // Act
    var retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

    // Assert
    Assert.NotNull(retrievedBooking);
    Assert.Equal(createdBooking.Id, retrievedBooking.Id);
    Assert.Equal(createdBooking.EventId, retrievedBooking.EventId);
    Assert.Equal(createdBooking.Status, retrievedBooking.Status);
    Assert.Equal(createdBooking.CreatedAt, retrievedBooking.CreatedAt);
    Assert.Equal(createdBooking.ProcessedAt, retrievedBooking.ProcessedAt);
  }

  /// <summary>
  /// Получить статус брони до и после обновления
  /// </summary>
  [Fact]
  public async Task GetBookingById_WithUpdateStatus_ShouldReflectStatusChangeAfterProcessing()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие и бронь
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);
    var createdBooking = await bookingService.CreateBookingAsync(createdEvent.Id);

    // Проверить первоначальный статус
    var bookingBefore = await bookingService.GetBookingByIdAsync(createdBooking.Id);
    Assert.NotNull(bookingBefore);
    Assert.Equal(BookingStatus.Pending, bookingBefore.Status);
    Assert.Null(bookingBefore.ProcessedAt);

    // Act - обновить статус
    var updateSuccess = await bookingService.UpdateBookingStatusAsync(createdBooking.Id, BookingStatus.Confirmed);

    // Assert - проверить обновленный статус
    Assert.True(updateSuccess);

    var bookingAfter = await bookingService.GetBookingByIdAsync(createdBooking.Id);
    Assert.NotNull(bookingAfter);
    Assert.Equal(BookingStatus.Confirmed, bookingAfter.Status);
    Assert.NotNull(bookingAfter.ProcessedAt);
  }

  /// <summary>
  /// Получить только новые брони (после обновления статуса одной из броней)
  /// </summary>
  [Fact]
  public async Task GetPendingBookings_ShouldReturnOnlyPendingBookings()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);

    // Создать несколько броней
    var booking1 = await bookingService.CreateBookingAsync(createdEvent.Id);
    var booking2 = await bookingService.CreateBookingAsync(createdEvent.Id);
    var booking3 = await bookingService.CreateBookingAsync(createdEvent.Id);

    // Подтвердить одну бронь
    await bookingService.UpdateBookingStatusAsync(booking2.Id, BookingStatus.Confirmed);

    // Act
    var pendingBookings = await bookingService.GetBookingByStatusAsync(BookingStatus.Pending);

    // Assert
    Assert.NotNull(pendingBookings);
    var pendingList = pendingBookings.ToList();
    Assert.Equal(2, pendingList.Count);
    Assert.Contains(pendingList, b => b.Id == booking1.Id);
    Assert.Contains(pendingList, b => b.Id == booking3.Id);
    Assert.DoesNotContain(pendingList, b => b.Id == booking2.Id);
    Assert.All(pendingList, b => Assert.Equal(BookingStatus.Pending, b.Status));
  }

  /// <summary>
  /// Создать бронь, получить бронь. Параметры созданной брони равны полученной
  /// </summary>
  [Fact]
  public async Task CreateBooking_ThenGetBooking_ShouldReturnSameBooking()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);

    // Act - создать бронь
    var createdBooking = await bookingService.CreateBookingAsync(createdEvent.Id);

    // Act - получить бронь
    var retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

    // Assert
    Assert.NotNull(retrievedBooking);
    Assert.Equal(createdBooking.Id, retrievedBooking.Id);
    Assert.Equal(createdBooking.EventId, retrievedBooking.EventId);
    Assert.Equal(createdBooking.Status, retrievedBooking.Status);
    Assert.Equal(createdBooking.CreatedAt, retrievedBooking.CreatedAt);
    Assert.Equal(createdBooking.ProcessedAt, retrievedBooking.ProcessedAt);
  }

  /// <summary>
  /// Содание броней одновременно
  /// </summary>
  [Fact]
  public async Task ConcurrentBookings_ShouldBeProcessedCorrectly()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);

    // Act - создать несколько броней
    var tasks = new List<Task<BookingDTO>>();
    for (int i = 0; i < 10; i++)
    {
      tasks.Add(bookingService.CreateBookingAsync(createdEvent.Id));
    }

    var bookings = await Task.WhenAll(tasks);

    // Assert
    Assert.Equal(10, bookings.Length);
    Assert.All(bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));

    // Проверить уникальность ID
    var uniqueIds = bookings.Select(b => b.Id).Distinct();
    Assert.Equal(10, uniqueIds.Count());
  }
  #endregion

  #region Неуспешные сценарии

  /// <summary>
  /// Создать бронь для несуществующего мероприятия
  /// </summary>
  [Fact]
  public async Task CreateBooking_ForNonExistentEvent_ShouldThrowNotFoundException()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);
    var nonExistentEventId = Guid.NewGuid();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
        await bookingService.CreateBookingAsync(nonExistentEventId));

    Assert.Contains(nonExistentEventId.ToString(), exception.Message);
  }

  /// <summary>
  /// Создать бронь для удаленного мероприятия
  /// </summary>
  [Fact]
  public async Task CreateBooking_ForDeletedEvent_ShouldThrowNotFoundException()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);
    var eventId = createdEvent.Id;

    // Удалить событие
    eventService.Delete(eventId);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
        await bookingService.CreateBookingAsync(eventId));

    Assert.Contains(eventId.ToString(), exception.Message);
  }

  /// <summary>
  /// Получить бронь по несуществующему Id
  /// </summary>
  [Fact]
  public async Task GetBookingById_WithNonExistentId_ShouldThrowNotFoundException()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);
    var nonExistentBookingId = Guid.NewGuid();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
        await bookingService.GetBookingByIdAsync(nonExistentBookingId));
    Assert.Contains($"Entity 'Booking' with id '{nonExistentBookingId}' was not found", exception.Message);
  }

  /// <summary>
  /// Обновить стаус несуществующей брони
  /// </summary>
  [Fact]
  public async Task UpdateBookingStatus_ForNonExistentBooking_ShouldReturnFalse()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);
    var nonExistentBookingId = Guid.NewGuid();

    // Act
    var result = await bookingService.UpdateBookingStatusAsync(nonExistentBookingId, BookingStatus.Confirmed);

    // Assert
    Assert.False(result);
  }

  /// <summary>
  /// Обновить статус уже подтвержденной брони
  /// </summary>
  [Fact]
  public async Task UpdateBookingStatus_ForAlreadyProcessedBooking_ShouldReturnFalse()
  {
    // Arrange
    var eventService = new EventService();
    var logger = new NullLogger<BookingService>();
    var bookingService = new BookingService(eventService, logger);

    // Создать событие и бронь
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = eventService.Create(createEventDto);
    var booking = await bookingService.CreateBookingAsync(createdEvent.Id);

    // Подтвердить бронь
    await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Confirmed);

    // Act - снова обновить статус
    var result = await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Rejected);

    // Assert
    Assert.False(result);

    // Проверить что статус не изменился
    var updatedBooking = await bookingService.GetBookingByIdAsync(booking.Id);
    Assert.NotNull(updatedBooking);
    Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
  }

  #endregion
}