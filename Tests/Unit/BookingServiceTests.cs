using Moq;
using Xunit;
using EventManagement.Application.Services;
using EventManagement.Application.Ports;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Enums;
using EventManagement.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagement.Tests.Services;

public class BookingServiceTests
{
  private readonly Mock<IEventRepository> _eventRepoMock;
  private readonly Mock<IBookingRepository> _bookingRepoMock;
  private readonly Mock<IUserRepository> _userRepoMock;
  private readonly BookingService _service;
  
  public BookingServiceTests()
  {
    _eventRepoMock = new Mock<IEventRepository>();
    _bookingRepoMock = new Mock<IBookingRepository>();
    _userRepoMock = new Mock<IUserRepository>();
    var logger = new NullLogger<BookingService>();

    _service = new BookingService(
        _bookingRepoMock.Object,
        _eventRepoMock.Object,
        _userRepoMock.Object,
        logger);
  }

  [Fact]
  public async Task CreateBookingAsync_WhenEventHasAlreadyStarted_ShouldThrowEventAlreadyStartedException()
  {
    // Arrange
    var eventId = Guid.NewGuid();
    var userId = Guid.NewGuid();    

    var eventItem = new Event(
        "Past Event",
        DateTime.UtcNow.AddDays(-5),
        DateTime.UtcNow.AddDays(-3),
        10);
    
    var user = new User(
        "login_user",
        "password_hash",
        Role.Admin
    );

    // рефлексия для установки Id
    typeof(Event).GetProperty("Id")?.SetValue(eventItem, eventId);
    typeof(User).GetProperty("Id")?.SetValue(user, userId);

    _eventRepoMock
        .Setup(r => r.GetByIdAsync(eventId))
        .ReturnsAsync(eventItem);

    // Act
    Task Act() => _service.CreateBookingAsync(eventId, userId);

    // Assert
    var exception = await Assert.ThrowsAsync<EventAlreadyStartedException>(Act);
    Assert.Contains("has already started", exception.Message);
  }

  
  // При достижении лимита броней новая бронь не создаётся
  [Fact]
  public async Task CreateBookingAsync_WhenUserHasReachedBookingLimit_ShouldThrowBookingLimitExceededException()
  {
    // Arrange
    var eventId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var maxBookings = 10;

    var eventItem = new Event(
        "Test Event",
        DateTime.UtcNow.AddDays(30),
        DateTime.UtcNow.AddDays(34),
        10);

    typeof(Event).GetProperty("Id")?.SetValue(eventItem, eventId);

    _eventRepoMock
        .Setup(r => r.GetByIdAsync(eventId))
        .ReturnsAsync(eventItem);

    // Пользователь уже имеет максимальное количество активных броней
    _userRepoMock
        .Setup(r => r.CountActiveBookingsAsync(userId))
        .ReturnsAsync(maxBookings);

    // Act
    Task Act() => _service.CreateBookingAsync(eventId, userId);

    // Assert
    var exception = await Assert.ThrowsAsync<BookingLimitExceededException>(Act);
    Assert.Contains($"{maxBookings}", exception.Message);
    Assert.Equal(maxBookings, exception.Limit);
  }

  [Fact]
  public async Task CreateBookingAsync_WhenUserHasNoActiveBookings_ShouldSucceed()
  {
    // Arrange
    var eventId = Guid.NewGuid();
    var userId = Guid.NewGuid();

    var eventItem = new Event(
        "Test Event",
        DateTime.UtcNow.AddDays(30),
        DateTime.UtcNow.AddDays(34),
        10);

    typeof(Event).GetProperty("Id")?.SetValue(eventItem, eventId);

    _eventRepoMock
        .Setup(r => r.GetByIdAsync(eventId))
        .ReturnsAsync(eventItem);

    _eventRepoMock
        .Setup(r => r.TryReserveSeatsAsync(eventId, 1))
        .ReturnsAsync(true);

    _userRepoMock
        .Setup(r => r.CountActiveBookingsAsync(userId))
        .ReturnsAsync(0);

    _bookingRepoMock
        .Setup(r => r.CreateAsync(It.IsAny<Booking>()))
        .ReturnsAsync(new Booking(eventId, userId));

    // Act
    var result = await _service.CreateBookingAsync(eventId, userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(eventId, result.EventId);
    Assert.Equal(userId, result.UserId);
  }

  // Лимиты разных пользователей не влияют друг на друга
  [Fact]
  public async Task CreateBookingAsync_MultipleUsers_EachHasOwnLimitIndependently()
  {
    // Arrange
    var eventId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    var user3Id = Guid.NewGuid();
    var maxBookings = 10;

    var eventItem = new Event(
        "Test Event",
        DateTime.UtcNow.AddDays(30),
        DateTime.UtcNow.AddDays(34),
        30);

    typeof(Event).GetProperty("Id")?.SetValue(eventItem, eventId);

    _eventRepoMock
        .Setup(r => r.GetByIdAsync(eventId))
        .ReturnsAsync(eventItem);

    _eventRepoMock
        .Setup(r => r.TryReserveSeatsAsync(eventId, 1))
        .ReturnsAsync(true);

    // Разные пользователи с разным количеством активных броней
    _userRepoMock
        .Setup(r => r.CountActiveBookingsAsync(user1Id))
        .ReturnsAsync(maxBookings); // На лимите

    _userRepoMock
        .Setup(r => r.CountActiveBookingsAsync(user2Id))
        .ReturnsAsync(5); // Половина лимита

    _userRepoMock
        .Setup(r => r.CountActiveBookingsAsync(user3Id))
        .ReturnsAsync(8); // Почти на лимите

    _bookingRepoMock
        .Setup(r => r.CreateAsync(It.Is<Booking>(b => b.UserId != user1Id)))
        .ReturnsAsync((Booking b) => new Booking(b.EventId, b.UserId));

    // Act & Assert
    // User1 - на лимите, не может создать бронь
    Task ActUser1() => _service.CreateBookingAsync(eventId, user1Id);
    await Assert.ThrowsAsync<BookingLimitExceededException>(ActUser1);

    // User2 - может создать бронь
    var resultUser2 = await _service.CreateBookingAsync(eventId, user2Id);
    Assert.NotNull(resultUser2);
    Assert.Equal(user2Id, resultUser2.UserId);

    // User3 - может создать бронь
    var resultUser3 = await _service.CreateBookingAsync(eventId, user3Id);
    Assert.NotNull(resultUser3);
    Assert.Equal(user3Id, resultUser3.UserId);
  }

  [Fact]
  public async Task CreateBookingAsync_TwoUsers_User1AtLimit_User2NotAtLimit()
  {
    // Arrange
    var eventId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    var maxBookings = 10;

    var eventItem = new Event(
        "Test Event",
        DateTime.UtcNow.AddDays(30),
        DateTime.UtcNow.AddDays(34),
        20);

    var user1 = new User(
        "login_user1",
        "password_hash1",
        Role.User
    ); 

    var user2 = new User(
        "login_user2",
        "password_hash2",
        Role.User
    );            

    typeof(Event).GetProperty("Id")?.SetValue(eventItem, eventId);
    typeof(User).GetProperty("Id")?.SetValue(user1, user1Id);
    typeof(User).GetProperty("Id")?.SetValue(user2, user2Id);

    _eventRepoMock
        .Setup(r => r.GetByIdAsync(eventId))
        .ReturnsAsync(eventItem);

    _eventRepoMock
        .Setup(r => r.TryReserveSeatsAsync(eventId, 1))
        .ReturnsAsync(true);

    // User1 на лимите, User2 не на лимите
    _userRepoMock
        .Setup(r => r.CountActiveBookingsAsync(user1Id))
        .ReturnsAsync(maxBookings);

    _userRepoMock
        .Setup(r => r.CountActiveBookingsAsync(user2Id))
        .ReturnsAsync(3);

    _bookingRepoMock
        .Setup(r => r.CreateAsync(It.Is<Booking>(b => b.UserId == user2Id)))
        .ReturnsAsync(new Booking(eventId, user2Id));

    // Act
    Task Act1() => _service.CreateBookingAsync(eventId, user1Id);    
    Task Act2() => _service.CreateBookingAsync(eventId, user2Id);
    

    // Assert
    // User1 должен получить исключение
    var result1 = await Assert.ThrowsAsync<BookingLimitExceededException>(Act1);  
    // User2 должен успешно создать бронь  
    var result2 = await _service.CreateBookingAsync(eventId, user2Id); 
  }
}