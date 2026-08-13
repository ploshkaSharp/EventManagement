using EventManagement.Events.Application.DTOs;
using EventManagement.Bookings.Application.DTOs;

namespace EventManagement.Tests;

/// <summary>
/// Генератор тестовых данных
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Сгенерировать тестовые события
    /// </summary>
    public static List<CreateEventDTO> GetTestEvents()
    {
        return new List<CreateEventDTO>
        {            
            new CreateEventDTO()
            {
                Title = "HouseHold Expo 2030",
                Description = "Technology conference",
                StartAt = new DateTime(2030, 3, 17, 10, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 3, 19, 18, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO()
            {
                Title = "Composit expo 2030",
                Description = "Business review",
                StartAt = new DateTime(2030, 6, 10, 14, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 6, 12, 16, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO()
            {
                Title = "Baltic Rally",
                Description = "Summer celebration",
                StartAt = new DateTime(2030, 7, 20, 19, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 7, 20, 23, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO()
            {
                Title = "Tomorrowland Thailand",
                Description = "Retail forum",
                StartAt = new DateTime(2030, 8, 5, 9, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2030, 8, 5, 17, 0, 0, DateTimeKind.Utc),
                TotalSeats = 10
            },
            new CreateEventDTO()
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
    /// Создать событие (валидное)
    /// </summary>
    public static CreateEventDTO GetValidCreateEventDTO()
    {
        return new CreateEventDTO(){
            Title = $"New Test Event {Guid.NewGuid()}",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(34),
            TotalSeats = 10
        };
    }

    /// <summary>
    /// Обновить событие (валидное)
    /// </summary>
    public static UpdateEventDTO GetValidUpdateEventDto()
    {
        return new UpdateEventDTO()
        {
            Title = $"Updated Test Event {Guid.NewGuid()}",
            Description = "Updated Description",
            StartAt = DateTime.UtcNow.AddDays(45),
            EndAt = DateTime.UtcNow.AddDays(46).AddHours(5)
        };
    }

    /// <summary>
    /// Создать тестовое событие с указанным количеством мест
    /// </summary>
    public static CreateEventDTO CreateTestEventWithSeats(int totalSeats)
    {
        return new CreateEventDTO(){
            Title = $"Test Event {Guid.NewGuid()}",
            Description = "Test Description",
            StartAt = DateTime.UtcNow.AddDays(30),
            EndAt = DateTime.UtcNow.AddDays(34),
            TotalSeats = totalSeats
        };
    }
}