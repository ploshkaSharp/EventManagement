using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using EventManagement.Infrastructure.Data;
using EventManagement.Application.Ports;
using EventManagement.Application.Services;
using EventManagement.Application.DTOs;
using EventManagement.Domain.Exceptions;
using EventManagement.Domain.Enums;
using EventManagement.Infrastructure.Repositories;
using EventManagement.Domain.Entities;

namespace EventManagement.Tests.Services;

public class BookingServiceSeatsTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;

    public BookingServiceSeatsTests()
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

    #region Успешные сценарии

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var userId = Guid.NewGuid();
        

        var createEventDto = new CreateEventDTO
        {
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 10
        };

        var createdEvent = await eventService.CreateAsync(createEventDto);

        // Act
        var booking = await bookingService.CreateBookingAsync(createdEvent.Id, userId);

        // Assert
        var updatedEvent = await eventService.GetByIdAsync(createdEvent.Id);
        Assert.NotNull(updatedEvent);
        Assert.Equal(9, updatedEvent.AvailableSeats);
        Assert.Equal(10, updatedEvent.TotalSeats);
    }

    [Fact]
    public async Task CreateMultipleBookings_UntilLimit_ShouldAllSucceed()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var userId = Guid.NewGuid();

        var createEventDto = new CreateEventDTO
        {
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 5
        };

        var createdEvent = await eventService.CreateAsync(createEventDto);
        var bookings = new List<BookingDTO>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var booking = await bookingService.CreateBookingAsync(createdEvent.Id, userId);
            bookings.Add(booking);
        }

        // Assert
        Assert.Equal(5, bookings.Count);
        var uniqueIds = bookings.Select(b => b.Id).Distinct();
        Assert.Equal(5, uniqueIds.Count());

        var updatedEvent = await eventService.GetByIdAsync(createdEvent.Id);
        Assert.NotNull(updatedEvent);
        Assert.Equal(0, updatedEvent.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_WhenNoSeatsLeft_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var userId = Guid.NewGuid();

        var createEventDto = new CreateEventDTO
        {
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 1
        };

        var createdEvent = await eventService.CreateAsync(createEventDto);
        await bookingService.CreateBookingAsync(createdEvent.Id, userId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            bookingService.CreateBookingAsync(createdEvent.Id, userId));

        Assert.Contains("No available seats", exception.Message);
    }

    #endregion

    #region Неуспешные сценарии

    [Fact]
    public async Task CreateBooking_ForNonExistentEvent_ShouldThrowNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.NewGuid(), userId));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenNoSeats_ShouldThrowValidationException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createEventDto = new CreateEventDTO
        {
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 0
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            eventService.CreateAsync(createEventDto));

        Assert.Contains("TotalSeats must be greater than 0", exception.Message);
    }

    #endregion

    #region Тесты на смену статуса

    [Fact]
    public async Task ConfirmBooking_ShouldSetStatusToConfirmedAndSetProcessedAt()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var userId = Guid.NewGuid();

        var createEventDto = new CreateEventDTO
        {
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 10
        };

        var createdEvent = await eventService.CreateAsync(createEventDto);
        var booking = await bookingService.CreateBookingAsync(createdEvent.Id, userId);

        // Act
        var success = await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Confirmed);

        // Assert
        Assert.True(success);
        var updatedBooking = await bookingService.GetBookingByIdAsync(booking.Id, userId, false);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }

    [Fact]
    public async Task RejectBooking_ShouldReleaseSeats()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var userId = Guid.NewGuid();

        var createEventDto = new CreateEventDTO
        {
            Title = $"Test Event {Guid.NewGuid()}",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 5
        };

        var createdEvent = await eventService.CreateAsync(createEventDto);
        var booking = await bookingService.CreateBookingAsync(createdEvent.Id, userId);

        var beforeReject = await eventService.GetByIdAsync(createdEvent.Id);        
        Assert.NotNull(beforeReject);
        Assert.Equal(4, beforeReject.AvailableSeats);

        // Act
        var success = await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Rejected);
        await eventService.ReleaseSeatsAsync(beforeReject.Id);

        // Assert
        Assert.True(success);

        var afterReject = await eventService.GetByIdAsync(createdEvent.Id);
        Assert.NotNull(afterReject);
        Assert.Equal(5, afterReject.AvailableSeats);
        
        var updatedBooking = await bookingService.GetBookingByIdAsync(booking.Id, userId, false);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Rejected, updatedBooking.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);

        // Можно создать новую бронь
        var newBooking = await bookingService.CreateBookingAsync(createdEvent.Id, userId);
        Assert.NotNull(newBooking);
    }

    #endregion

    #region Тесты на конкурентность
    [Fact]
    public async Task ConcurrentBookings_ShouldProtectAgainstOverbooking()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var userId = Guid.NewGuid();

        var createEventDto = new CreateEventDTO
        {
            Title = "Concurrency Test Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 5
        };

        var createdEvent = await eventService.CreateAsync(createEventDto);
        var eventId = createdEvent.Id;

        // Act - 20 конкурентных запросов с отдельными scope
        var tasks = new List<Task<BookingDTO>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {                
                using var innerScope = _serviceProvider.CreateScope();
                var innerBookingService = innerScope.ServiceProvider.GetRequiredService<IBookingService>();
                return await innerBookingService.CreateBookingAsync(eventId, userId);                    
            }));
        }

        // Assert
        var exceptions = new List<Exception>();
        var successfulBookings = new List<BookingDTO>();

        foreach (var task in tasks)
        {
            try
            {
                var result = await task;
                successfulBookings.Add(result);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        Assert.Equal(5, successfulBookings.Count);
        var noSeatExceptions = exceptions.Count(e => e is NoAvailableSeatsException);
        Assert.Equal(15, noSeatExceptions);        

        // Проверить конечное состояние
        using var finalScope = _serviceProvider.CreateScope();
        var finalEventService = finalScope.ServiceProvider.GetRequiredService<IEventService>();
        var finalEvent = await finalEventService.GetByIdAsync(eventId);
        Assert.NotNull(finalEvent);
        Assert.Equal(0, finalEvent.AvailableSeats);      
    }

    [Fact]
    public async Task ConcurrentBookings_ShouldGenerateUniqueIds()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var userId = Guid.NewGuid();

        var createEventDto = new CreateEventDTO
        {
            Title = "Unique Id Test Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = 10
        };

        var createdEvent = await eventService.CreateAsync(createEventDto);
        var eventId = createdEvent.Id;

        // Act - 10 конкурентных запросов с отдельными scope
        var tasks = new List<Task<BookingDTO>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var innerScope = _serviceProvider.CreateScope();
                var innerBookingService = innerScope.ServiceProvider.GetRequiredService<IBookingService>();
                return await innerBookingService.CreateBookingAsync(eventId, userId);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var bookingIds = results.Select(b => b.Id).ToList();
        var uniqueIds = bookingIds.Distinct().ToList();

        Assert.Equal(10, bookingIds.Count);
        Assert.Equal(10, uniqueIds.Count);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidTotalSeats_ShouldThrowValidationException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createEventDto = new CreateEventDTO
        {
            Title = "Invalid Seats Event",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(30).AddHours(4),
            TotalSeats = -5
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            eventService.CreateAsync(createEventDto));

        Assert.Contains("TotalSeats must be greater than 0", exception.Message);
    }

    #endregion

    public void Dispose()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureDeleted();
        _serviceProvider.Dispose();
    }
}