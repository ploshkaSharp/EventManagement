using Xunit;
using EventManagement.Models;
using EventManagement.Repositories;
using EventManagement.IntegrationTests.Base;
using Microsoft.Extensions.DependencyInjection;

namespace EventManagement.IntegrationTests.Repositories;

/// <summary>
/// Интеграционные тесты бронирования
/// </summary>
public class BookingRepositoryTests : IntegrationTestBase
{
  /// <summary>
  /// Создание
  /// </summary>  
  [Fact]
  public async Task CreateAsync_ShouldAddBookingToDatabase()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var createdEvent = await eventRepository.CreateAsync(eventItem);

    var booking = new Booking(createdEvent.Id);

    // Act
    var result = await bookingRepository.CreateAsync(booking);

    // Assert
    Assert.NotNull(result);
    Assert.NotEqual(Guid.Empty, result.Id);
    Assert.Equal(createdEvent.Id, result.EventId);
    Assert.Equal(BookingStatus.Pending, result.Status);
  }

  /// <summary>
  /// получить по ИД
  /// </summary>
  [Fact]
  public async Task GetByIdAsync_WithExistingId_ShouldReturnBooking()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var createdEvent = await eventRepository.CreateAsync(eventItem);
    var booking = new Booking(createdEvent.Id);
    var created = await bookingRepository.CreateAsync(booking);

    // Act
    var result = await bookingRepository.GetByIdAsync(created.Id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(created.Id, result.Id);
    Assert.Equal(created.EventId, result.EventId);
  }

  /// <summary>
  /// Получить по ИД мероприятия
  /// </summary>
  [Fact]
  public async Task GetByEventIdAsync_ShouldReturnBookingsForSpecificEvent()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var createdEvent = await eventRepository.CreateAsync(eventItem);

    for (int i = 0; i < 5; i++)
    {
      var booking = new Booking(createdEvent.Id);
      await bookingRepository.CreateAsync(booking);
    }

    // Act
    var result = await bookingRepository.GetByEventIdAsync(createdEvent.Id);

    // Assert
    Assert.Equal(5, result.Count());
    Assert.All(result, b => Assert.Equal(createdEvent.Id, b.EventId));
  }

  /// <summary>
  /// Получить по статусу (Pending)
  /// </summary>  
  [Fact]
  public async Task GetPendingBookingsAsync_ShouldReturnOnlyPendingBookings()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var createdEvent = await eventRepository.CreateAsync(eventItem);

    var booking1 = new Booking(createdEvent.Id);
    var booking2 = new Booking(createdEvent.Id);
    var booking3 = new Booking(createdEvent.Id);

    var created1 = await bookingRepository.CreateAsync(booking1);
    var created2 = await bookingRepository.CreateAsync(booking2);
    var created3 = await bookingRepository.CreateAsync(booking3);

    created2.Confirm();
    await bookingRepository.UpdateAsync(created2);

    // Act
    var result = await bookingRepository.GetBookingByStatusAsync(BookingStatus.Pending);

    // Assert
    Assert.Equal(2, result.Count());
    Assert.Contains(result, b => b.Id == created1.Id);
    Assert.Contains(result, b => b.Id == created3.Id);
    Assert.DoesNotContain(result, b => b.Id == created2.Id);
    Assert.All(result, b => Assert.Equal(BookingStatus.Pending, b.Status));
  }

  /// <summary>
  /// Обновить 
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task UpdateAsync_ShouldModifyBookingStatus()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var createdEvent = await eventRepository.CreateAsync(eventItem);
    var booking = new Booking(createdEvent.Id);
    var created = await bookingRepository.CreateAsync(booking);

    created.Confirm();

    // Act
    var result = await bookingRepository.UpdateAsync(created);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(BookingStatus.Confirmed, result.Status);
    Assert.NotNull(result.ProcessedAt);
  }

  /// <summary>
  /// Удалить бронь
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task DeleteAsync_ShouldRemoveBooking()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var createdEvent = await eventRepository.CreateAsync(eventItem);
    var booking = new Booking(createdEvent.Id);
    var created = await bookingRepository.CreateAsync(booking);

    // Act
    var result = await bookingRepository.DeleteAsync(created.Id);

    // Assert
    Assert.True(result);
    var deleted = await bookingRepository.GetByIdAsync(created.Id);
    Assert.Null(deleted);
  }
}