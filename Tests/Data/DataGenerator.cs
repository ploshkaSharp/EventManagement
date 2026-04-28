using EventManagement.DTOs;
using EventManagement.Services;

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
        var localOffset = new TimeSpan(+4, 0, 0);
        return new List<CreateEventDTO>
        {
            new CreateEventDTO
            {
                Title = "HouseHold Expo 2030",
                Description = "Technology conference",
                StartAt = new DateTimeOffset(2030, 3, 17, 10, 0, 0, localOffset),
                EndAt = new DateTimeOffset(2030, 3, 19, 18, 0, 0, localOffset)
            },
            new CreateEventDTO
            {
                Title = "Composit expo 2030",
                Description = "Business review",
                StartAt = new DateTimeOffset(2030, 6, 10, 14, 0, 0, localOffset),
                EndAt = new DateTimeOffset(2030, 6, 12, 16, 0, 0, localOffset)
            },
            new CreateEventDTO
            {
                Title = "Baltic Rally",
                Description = "Summer celebration",
                StartAt = new DateTimeOffset(2030, 7, 20, 19, 0, 0, localOffset),
                EndAt = new DateTimeOffset(2030, 7, 20, 23, 0, 0, localOffset)
            },
            new CreateEventDTO
            {
                Title = "Tomorrowland Thailand",
                Description = "Retail forum",
                StartAt = new DateTimeOffset(2030, 8, 5, 9, 0, 0, localOffset),
                EndAt = new DateTimeOffset(2030, 8, 5, 17, 0, 0, localOffset)
            },
            new CreateEventDTO
            {
                Title = "Wild Siberia Extreme Triathlon",
                Description = "Sport event",
                StartAt = new DateTimeOffset(2030, 9, 12, 10, 0, 0, localOffset),
                EndAt = new DateTimeOffset(2030, 9, 12, 18, 0, 0, localOffset)
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
            Title = "New Test Event",
            Description = "Test Description",
            StartAt = DateTimeOffset.Now.AddDays(30),
            EndAt = DateTimeOffset.Now.AddDays(34)
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
            Title = "Updated Test Event",
            Description = "Updated Description",
            StartAt = DateTimeOffset.Now.AddDays(45),
            EndAt = DateTimeOffset.Now.AddDays(46).AddHours(5)
        };
    }

    /// <summary>
    /// Создать "пустой" экземпляр сервиса мероприятий
    /// </summary>
    /// <returns>"Пустой" экземпляр сервиса мероприятий</returns>
    public static EventService CreateFreshEventService()
    {
        return new EventService();
    }

    /// <summary>
    /// Создать экземпляр сервиса мероприятия с тестовыми данными
    /// </summary>
    /// <returns>экземпляр сервиса мероприятия с тестовыми мероприятиями</returns>
    public static EventService CreateEventServiceWithSeedData()
    {
        var service = new EventService();
        var events = GetTestEvents();

        foreach (var eventDto in events)
        {
            service.Create(eventDto);
        }

        return service;
    }
}