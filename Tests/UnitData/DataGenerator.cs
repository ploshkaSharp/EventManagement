using EventManagement.Application.DTOs;
using EventManagement.Application.Services;
using EventManagement.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Tests;

/// <summary>
/// Генератор тестовых данных
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Сгенерировать тестовые данные
    /// </summary>
    /// <returns>Тестовые мероприятия</returns>
    public static List<CreateEventDTO> GetTestEvents()
    {
        return new List<CreateEventDTO>
        {
            new CreateEventDTO
            {
                Title = "HouseHold Expo 2030",
                Description = "Technology conference",
                StartAt = new DateTime(2030, 3, 17, 10, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 3, 19, 18, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO
            {
                Title = "Composit expo 2030",
                Description = "Business review",
                StartAt = new DateTime(2030, 6, 10, 14, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 6, 12, 16, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO
            {
                Title = "Baltic Rally",
                Description = "Summer celebration",
                StartAt = new DateTime(2030, 7, 20, 19, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 7, 20, 23, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO
            {
                Title = "Tomorrowland Thailand",
                Description = "Retail forum",
                StartAt = new DateTime(2030, 8, 5, 9, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 8, 5, 17, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO
            {
                Title = "Wild Siberia Extreme Triathlon",
                Description = "Sport event",
                StartAt = new DateTime(2030, 9, 12, 10, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 9, 12, 18, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            }
        };
    }

    /// <summary>
    /// Создать мероприятие (валидно)
    /// </summary>
    /// <returns>Созданное тестовое мероприятие (без ошибок)</returns>
    public static CreateEventDTO GetValidCreateEventDto()
    {
        return new CreateEventDTO
        {
            Title = $"New Test Event {Guid.NewGuid()}",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(34),
            TotalSeats = 10
        };
    }

    /// <summary>
    /// Обновить мероприятие (валидно)
    /// </summary>
    /// <returns>Обновленное тестовое мероприятие (без ошибок)</returns>
    public static UpdateEventDTO GetValidUpdateEventDto()
    {
        return new UpdateEventDTO
        {
            Title = $"Updated Test Event {Guid.NewGuid()}",
            Description = "Updated Description",
            StartAt = DateTime.UtcNow.AddDays(45),
            EndAt = DateTime.UtcNow.AddDays(46).AddHours(5)
        };
    }

    /// <summary>
    /// Создать ServiceProvider с InMemoryDatabase для тестов
    /// </summary>
    /// <returns>ServiceProvider с настроенным DI</returns>
    public static ServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));        
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddLogging();
        
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Создать ServiceProvider с InMemoryDatabase и заполненными тестовыми данными
    /// </summary>
    /// <returns>ServiceProvider с настроенным DI и тестовыми данными</returns>
    public static async Task<ServiceProvider> CreateTestServiceProviderWithSeedDataAsync()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddLogging();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Тестовые данные
        using var scope = serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var events = GetTestEvents();
        
        foreach (var eventDto in events)
        {
            await eventService.CreateAsync(eventDto);
        }
        
        return serviceProvider;
    }

    /// <summary>
    /// Создать тестовое событие с указанным количеством мест
    /// </summary>
    /// <param name="totalSeats">Общее количество мест</param>
    /// <returns>DTO для создания тестового события</returns>
    public static CreateEventDTO CreateTestEventWithSeats(int totalSeats)
    {
        return new CreateEventDTO
        {
            Title = $"Test Event {Guid.NewGuid()}",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(34),
            TotalSeats = totalSeats
        };
    }

    /// <summary>
    /// Создать тестовое событие с указанными параметрами
    /// </summary>
    /// <param name="title">Название события</param>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата окончания</param>
    /// <param name="totalSeats">Количество мест</param>
    /// <returns>DTO для создания тестового события</returns>
    public static CreateEventDTO CreateCustomTestEvent(string title, DateTime startAt, DateTime endAt, int totalSeats)
    {
        return new CreateEventDTO
        {
            Title = title,
            Description = $"Description for {title}",
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats
        };
    }

    /// <summary>
    /// Получить список тестовых бронирований
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="count">Количество бронирований</param>
    /// <returns>Список DTO для создания бронирований</returns>
    public static List<BookingDTO> GetTestBookings(Guid eventId, int count)
    {
        var bookings = new List<BookingDTO>();
        for (int i = 0; i < count; i++)
        {
            bookings.Add(new BookingDTO { EventId = eventId });
        }
        return bookings;
    }

    /// <summary>
    /// Очистка базы данных после теста
    /// </summary>
    /// <param name="serviceProvider">ServiceProvider для очистки</param>
    public static async Task CleanupDatabaseAsync(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
    }
}