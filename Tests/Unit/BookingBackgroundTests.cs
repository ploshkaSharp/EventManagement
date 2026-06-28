using Xunit;
using EventManagement.Application.Services;
using EventManagement.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using EventManagement.Infrastructure.Data;
using EventManagement.Application.Ports;
using EventManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Tests.Services;

public class BookingBackgroundServiceTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;
    public BookingBackgroundServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();        
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();        
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task BookingBackgroundService_ShouldProcessPendingBookingsAsync()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();        
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Создать событие
        var createEventDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = await eventService.CreateAsync(createEventDto);

        // Создать несколько броней
        var booking1 = await bookingService.CreateBookingAsync(createdEvent.Id);
        var booking2 = await bookingService.CreateBookingAsync(createdEvent.Id);
        var booking3 = await bookingService.CreateBookingAsync(createdEvent.Id);

        // Искусственно подтвердить бронь до обработки фоновым сервисом
        await bookingService.UpdateBookingStatusAsync(booking1.Id, BookingStatus.Confirmed);
        await bookingService.UpdateBookingStatusAsync(booking2.Id, BookingStatus.Confirmed);
        await bookingService.UpdateBookingStatusAsync(booking3.Id, BookingStatus.Confirmed);
        
        // Act - ождание обработки фоновым сервисом
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Assert - все брони должны быть обработаны
        var processedBooking1 = await bookingService.GetBookingByIdAsync(booking1.Id);
        var processedBooking2 = await bookingService.GetBookingByIdAsync(booking2.Id);
        var processedBooking3 = await bookingService.GetBookingByIdAsync(booking3.Id);

        Assert.NotNull(processedBooking1);
        Assert.NotNull(processedBooking2);
        Assert.NotNull(processedBooking3);

        Assert.Equal(BookingStatus.Confirmed, processedBooking1.Status);
        Assert.Equal(BookingStatus.Confirmed, processedBooking2.Status);
        Assert.Equal(BookingStatus.Confirmed, processedBooking3.Status);

        Assert.NotNull(processedBooking1.ProcessedAt);
        Assert.NotNull(processedBooking2.ProcessedAt);
        Assert.NotNull(processedBooking3.ProcessedAt);
    }    

    [Fact]
    public async Task BookingBackgroundService_ShouldNotProcessAlreadyConfirmedBookingsAsync()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();        
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Создать событие
        var createEventDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = await eventService.CreateAsync(createEventDto);

        // Создать бронь
        var booking = await bookingService.CreateBookingAsync(createdEvent.Id);

        // Искусственно подтвердить бронь до обработки фоновым сервисом
        await bookingService.UpdateBookingStatusAsync(booking.Id, BookingStatus.Confirmed);
        var processedAtBefore = (await bookingService.GetBookingByIdAsync(booking.Id))?.ProcessedAt;

        // Act - ожидать обработки фоновым сервисом
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert - бронь не должна быть обработана повторно
        var bookingAfter = await bookingService.GetBookingByIdAsync(booking.Id);
        Assert.NotNull(bookingAfter);
        Assert.Equal(BookingStatus.Confirmed, bookingAfter.Status);
        Assert.Equal(processedAtBefore, bookingAfter.ProcessedAt);
    }
}