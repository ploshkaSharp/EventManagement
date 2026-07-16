using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Infrastructure.Data;
using EventManagement.Application.Ports;
using EventManagement.Application.Services;
using EventManagement.Application.DTOs;
using EventManagement.Domain.Exceptions;
using EventManagement.Infrastructure.Repositories;

namespace EventManagement.Tests.Services;

/// <summary>
/// Тесты для EventService с использованием InMemory Database
/// </summary>
public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;

    public EventServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        services.AddScoped<IEventRepository, EventRepository>();        
        services.AddScoped<IEventService, EventService>();
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    #region Успешные сценарии

    /// <summary>
    /// Создание мероприятия с валидными данными
    /// </summary>
    [Fact]
    public async Task Create_WithValidData_ShouldCreateEvent()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = TestDataGenerator.GetValidCreateEventDto();

        // Act
        var result = await eventService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id.ToString());
        Assert.Equal(createDto.Title, result.Title);
        Assert.Equal(createDto.Description, result.Description);
        Assert.Equal(createDto.StartAt, result.StartAt);
        Assert.Equal(createDto.EndAt, result.EndAt);
        Assert.Equal(createDto.TotalSeats, result.TotalSeats);
        Assert.Equal(createDto.TotalSeats, result.AvailableSeats);

        var created = await eventService.GetByIdAsync(result.Id);
        Assert.NotNull(created);
        Assert.Equal(createDto.Title, created.Title);
    }

    /// <summary>
    /// Получить все мероприятия без фильтров
    /// </summary>
    [Fact]
    public async Task GetAll_WithoutFilters_ShouldReturnAllEvents()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);

        // Act
        var result = await eventService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count());
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].StartAt <= list[i + 1].StartAt);
        }
    }

    /// <summary>
    /// Получить существующее мероприятие по Guid
    /// </summary>
    [Fact]
    public async Task GetById_WithExistingId_ShouldReturnEvent()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = await eventService.CreateAsync(createDto);

        // Act
        var result = await eventService.GetByIdAsync(createdEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(createDto.Title, result.Title);
    }

    /// <summary>
    /// Обновить существующее мероприятие валидными данными
    /// </summary>
    [Fact]
    public async Task Update_WithValidData_ShouldUpdateEvent()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = await eventService.CreateAsync(createDto);
        var updateDto = TestDataGenerator.GetValidUpdateEventDto();

        // Act
        var result = await eventService.UpdateAsync(createdEvent.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(updateDto.Title, result.Title);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(updateDto.StartAt, result.StartAt);
        Assert.Equal(updateDto.EndAt, result.EndAt);

        var updated = await eventService.GetByIdAsync(createdEvent.Id);
        Assert.NotNull(updated);
        Assert.Equal(updateDto.Title, updated.Title);
    }

    /// <summary>
    /// Удалить существующее мероприятие по Guid
    /// </summary>
    [Fact]
    public async Task Delete_WithExistingId_ShouldDeleteEvent()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = await eventService.CreateAsync(createDto);

        // Act
        var result = await eventService.DeleteAsync(createdEvent.Id);

        // Assert
        Assert.True(result);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            eventService.GetByIdAsync(createdEvent.Id));
        Assert.Contains(createdEvent.Id.ToString(), exception.Message);
    }

    /// <summary>
    /// Получить мероприятия по фильтру (названию)
    /// </summary>
    [Fact]
    public async Task GetAll_WithTitleFilter_ShouldReturnMatchingEvents()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var filter = new EventFilterDto
        {
            Title = "expo"
        };

        // Act
        var result = await eventService.GetAllAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, e => e.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Получить мероприятия по фильтру (дате начала)
    /// </summary>
    [Fact]
    public async Task GetAll_WithFromDateFilter_ShouldReturnEventsStartingAfterDate()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var fromDate = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = new EventFilterDto
        {
            From = fromDate
        };

        // Act
        var result = await eventService.GetAllAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, e => Assert.True(e.StartAt >= fromDate));
    }

    /// <summary>
    /// Получить мероприятия по фильтру (дате окончания)
    /// </summary>
    [Fact]
    public async Task GetAll_WithToDateFilter_ShouldReturnEventsEndingBeforeDate()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var toDate = new DateTime(2030, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = new EventFilterDto
        {
            To = toDate
        };

        // Act
        var result = await eventService.GetAllAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, e => Assert.True(e.EndAt <= toDate));
    }

    /// <summary>
    /// Получить мероприятия по фильтрам (диапазон дата начала и дата конца)
    /// </summary>
    [Fact]
    public async Task GetAll_WithDateRangeFilter_ShouldReturnEventsInRange()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var fromDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2030, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = new EventFilterDto
        {
            From = fromDate,
            To = toDate
        };

        // Act
        var result = await eventService.GetAllAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, e =>
        {
            Assert.True(e.StartAt >= fromDate);
            Assert.True(e.EndAt <= toDate);
        });

        Assert.Contains(result, e => e.Title == "HouseHold Expo 2030");
        Assert.Contains(result, e => e.Title == "Composit expo 2030");
        Assert.DoesNotContain(result, e => e.Title == "Wild Siberia Extreme Triathlon");
    }

    /// <summary>
    /// Получить мероприятия по страницам (первая страница из двух мероприятий)
    /// </summary>
    [Fact]
    public async Task GetPaginated_WithFirstPage_ShouldReturnFirstPage()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var filter = new EventFilterDto
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = await eventService.GetPaginatedAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    /// <summary>
    /// Получить мероприятия по страницам (вторая страница из двух мероприятий)
    /// </summary>
    [Fact]
    public async Task GetPaginated_WithSecondPage_ShouldReturnCorrectItems()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var filter = new EventFilterDto
        {
            PageNumber = 2,
            PageSize = 2
        };

        // Act
        var result = await eventService.GetPaginatedAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    /// <summary>
    /// Получить мероприятия по страницам (последняя страница из одного мероприятия)
    /// </summary>
    [Fact]
    public async Task GetPaginated_WithLastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var filter = new EventFilterDto
        {
            PageNumber = 3,
            PageSize = 2
        };

        // Act
        var result = await eventService.GetPaginatedAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(3, result.PageNumber);
    }

    /// <summary>
    /// Получить мероприятия с комбо фильтром (диапазон дат, наименование)
    /// </summary>
    [Fact]
    public async Task GetAll_WithCombinedFilters_ShouldReturnEventsMatchingAllConditions()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var fromDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = new EventFilterDto
        {
            Title = "expo",
            From = fromDate,
            To = toDate
        };

        // Act
        var result = await eventService.GetAllAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, e =>
        {
            Assert.Contains(filter.Title, e.Title, StringComparison.OrdinalIgnoreCase);
            Assert.True(e.StartAt >= fromDate);
            Assert.True(e.EndAt <= toDate);
        });
        Assert.Equal("HouseHold Expo 2030", result.First().Title);
    }

    #endregion

    #region Неуспешные сценарии

    /// <summary>
    /// Получить мероприятие с несуществующим Guid
    /// </summary>
    [Fact]
    public async Task GetById_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            eventService.GetByIdAsync(nonExistentId));
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    /// <summary>
    /// Обновить мероприятие c неуществующим Guid
    /// </summary>
    [Fact]
    public async Task Update_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var nonExistentId = Guid.NewGuid();
        var updateDto = TestDataGenerator.GetValidUpdateEventDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            eventService.UpdateAsync(nonExistentId, updateDto));
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    /// <summary>
    /// Удалить мероприятие c неуществующим Guid
    /// </summary>
    [Fact]
    public async Task Delete_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            eventService.DeleteAsync(nonExistentId));
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    /// <summary>
    /// Создать мероприятие c пустым наименованием
    /// </summary>
    [Fact]
    public async Task Create_WithEmptyTitle_ShouldThrowArgumentException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = new CreateEventDTO
        {
            Title = string.Empty,
            Description = "Test",
            StartAt = DateTime.UtcNow.AddDays(10),
            EndAt = DateTime.UtcNow.AddDays(15),
            TotalSeats = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            eventService.CreateAsync(createDto));
        Assert.Contains("Title is required", exception.Message);
    }

    /// <summary>
    /// Создать мероприятие c null-наименованием
    /// </summary>
    [Fact]
    public async Task Create_WithNullTitle_ShouldThrowArgumentException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = new CreateEventDTO
        {
            Title = null!,
            Description = "Test",
            StartAt = DateTime.UtcNow.AddDays(10),
            EndAt = DateTime.UtcNow.AddDays(15),
            TotalSeats = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            eventService.CreateAsync(createDto));
        Assert.Contains("Title is required", exception.Message);
    }

    /// <summary>
    /// Создать мероприятие c уже существующим наименованием
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateTitle_ShouldThrowValidationException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await SeedTestEvents(eventService);
        var createDto = new CreateEventDTO
        {
            Title = "Composit expo 2030",
            Description = "Duplicate event",
            StartAt = DateTime.UtcNow.AddDays(10),
            EndAt = DateTime.UtcNow.AddDays(15),
            TotalSeats = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            eventService.CreateAsync(createDto));
        Assert.Contains("already exists", exception.Message);
    }

    /// <summary>
    /// Создать мероприятие с датой начала в прошлом
    /// </summary>
    [Fact]
    public async Task Create_WithStartDateInPast_ShouldThrowValidationException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = new CreateEventDTO
        {
            Title = "Past Event",
            Description = "Test",
            StartAt = DateTime.UtcNow.AddDays(-1),
            EndAt = DateTime.UtcNow.AddDays(1),
            TotalSeats = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            eventService.CreateAsync(createDto));
        Assert.Contains("StartAt must be more than now", exception.Message);
    }

    /// <summary>
    /// Создать мероприятие с датой начала старше, чем дата окончания
    /// </summary>
    [Fact]
    public async Task Create_WithEndDateBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var createDto = new CreateEventDTO
        {
            Title = "Invalid Dates Event",
            Description = "Test",
            StartAt = DateTime.UtcNow.AddDays(10),
            EndAt = DateTime.UtcNow.AddDays(8),
            TotalSeats = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => eventService.CreateAsync(createDto));
        Assert.Contains("StartAt must be less than EndAt", exception.Message);
    }

    #endregion

    #region Пограничные сценарии

    /// <summary>
    /// Получить пагинированный пустой список мероприятий
    /// </summary>
    [Fact]
    public async Task GetPaginated_WithZeroEvents_ShouldReturnEmptyResult()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var filter = new EventFilterDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await eventService.GetPaginatedAsync(filter);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestEvents(IEventService eventService)
    {
        var events = TestDataGenerator.GetTestEvents();
        foreach (var eventDto in events)
        {
            await eventService.CreateAsync(eventDto);
        }
    }

    public void Dispose()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureDeleted();
        _serviceProvider.Dispose();
    }

    #endregion
}