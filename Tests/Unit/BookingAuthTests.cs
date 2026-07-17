using Xunit;
using EventManagement.Application.Services;
using EventManagement.Domain.Enums;
using EventManagement.Domain.Exceptions;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EventManagement.Application.Ports;


namespace EventManagement.Tests.Services;

public class BookingAuthorizationTests
{
  private readonly ServiceProvider _serviceProvider;
  private readonly string _dbName;
  public BookingAuthorizationTests()
  {
    _dbName = Guid.NewGuid().ToString();
    var services = new ServiceCollection();
    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
    services.AddScoped<IEventRepository, EventRepository>();
    services.AddScoped<IBookingRepository, BookingRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IEventService, EventService>();
    services.AddScoped<IBookingService, BookingService>();
    services.AddLogging();

    _serviceProvider = services.BuildServiceProvider();
  }

  /// <summary>
  /// Отмена брони - владелец отменяет свою бронь
  /// </summary>
  [Fact]
  public async Task CancelBooking_OwnerCancelsOwnBooking_ShouldSucceed()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    var user = new User("Login_user", "Password_Hash", Role.User);
    var createdUser = await userRepository.CreateAsync(user);    
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = await eventService.CreateAsync(createEventDto);
    var booking = await bookingService.CreateBookingAsync(createdEvent.Id, createdUser.Id);

    // Act
    var result = await bookingService.CancelBookingAsync(booking.Id, createdUser.Id, false);

    // Assert
    Assert.True(result);
    // Проверить, что статус брони изменился на Cancelled
    var cancelledBooking = await bookingService.GetBookingByIdAsync(booking.Id, createdUser.Id, false);
    Assert.NotNull(cancelledBooking);
    Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);    
  }

  /// <summary>
  /// Отмена брони - пользователь не может отменить чужую бронь
  /// </summary>
  [Fact]
  public async Task CancelBooking_UserCancelsOtherUserBooking_ShouldThrowUnAuthorizedOperationException()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    var user1 = new User("Login_user1", "Password_Hash1", Role.User);
    var user2 = new User("Login_user2", "Password_Hash2", Role.User);    
    var createdUser1 = await userRepository.CreateAsync(user1);    
    var createdUser2 = await userRepository.CreateAsync(user2);     
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = await eventService.CreateAsync(createEventDto);
    var booking1 = await bookingService.CreateBookingAsync(createdEvent.Id, createdUser1.Id);
    
    // Act    
    Task Act() => bookingService.CancelBookingAsync(booking1.Id, createdUser2.Id, false);  

    // Assert
    // Чужую бронь пользователь отменить не может
    var exception = await Assert.ThrowsAsync<UnAuthorizedOperationException>(Act);
    Assert.Contains("only cancel their", exception.Message);    
  } 

  /// <summary>
  /// Отмена брони - администратор может отменить любую бронь
  /// </summary>
  [Fact]
  public async Task CancelBooking_AdminCancelsOtherUserBooking_ShouldSucceed()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    var user1 = new User("Login_user1", "Password_Hash1", Role.User);
    var user2 = new User("Login_user2", "Password_Hash2", Role.User);
    var admin = new User("Login_user3", "Password_Hash2", Role.Admin);
    var createdUser1 = await userRepository.CreateAsync(user1);    
    var createdUser2 = await userRepository.CreateAsync(user2); 
    var createdAdmin = await userRepository.CreateAsync(admin); 
    var createEventDto = TestDataGenerator.GetValidCreateEventDto();
    var createdEvent = await eventService.CreateAsync(createEventDto);
    var booking1 = await bookingService.CreateBookingAsync(createdEvent.Id, createdUser1.Id);
    var booking2 = await bookingService.CreateBookingAsync(createdEvent.Id, createdUser2.Id);    
    var booking3 = await bookingService.CreateBookingAsync(createdEvent.Id, createdAdmin.Id); 

    // Act
    var result1 = await bookingService.CancelBookingAsync(booking1.Id, createdAdmin.Id, true); 
    var result2 = await bookingService.CancelBookingAsync(booking2.Id, createdAdmin.Id, true);       
    var result3 = await bookingService.CancelBookingAsync(booking3.Id, createdAdmin.Id, true);

    // Assert
    Assert.True(result1);
    Assert.True(result2);
    Assert.True(result3);
    // Проверить, что все брони отменились - статус броней изменился на Cancelled
    var cancelledBooking1 = await bookingService.GetBookingByIdAsync(booking1.Id, createdAdmin.Id, true);
    var cancelledBooking2 = await bookingService.GetBookingByIdAsync(booking2.Id, createdAdmin.Id, true);
    var cancelledBooking3 = await bookingService.GetBookingByIdAsync(booking3.Id, createdAdmin.Id, true);

    Assert.NotNull(cancelledBooking1);
    Assert.Equal(BookingStatus.Cancelled, cancelledBooking1.Status);    
    Assert.NotNull(cancelledBooking2);
    Assert.Equal(BookingStatus.Cancelled, cancelledBooking2.Status);    
    Assert.NotNull(cancelledBooking3);
    Assert.Equal(BookingStatus.Cancelled, cancelledBooking3.Status);        
  }           
}