using Xunit;
using Moq;
using EventManagement.Events.Application.DTOs;
using EventManagement.Events.Application.Ports;
using EventManagement.Events.Application.Services;
using EventManagement.Events.Domain.Entities;
using EventManagement.Events.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EventManagement.Events.Application.Constants;

namespace EventManagement.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepoMock;
    private readonly EventService _eventService;
    private readonly Mock<ICacheService> _cacheMock;

    public EventServiceTests()
    {
        _cacheMock = new Mock<ICacheService>();
        _eventRepoMock = new Mock<IEventRepository>();
        var settings = Options.Create(new CacheSettings { Event = 600, Top10 = 300 });
        _eventService = new EventService(_eventRepoMock.Object, NullLogger<EventService>.Instance, _cacheMock.Object, settings);
    }

    #region Успешные сценарии

    [Fact]
    public async Task Create_WithValidData_ShouldCreateEvent()
    {
        // Arrange
        var createDto = TestDataGenerator.GetValidCreateEventDTO();
        var eventItem = new Event(createDto.Title, createDto.StartAt, createDto.EndAt, createDto.TotalSeats);

        _eventRepoMock.Setup(r => r.GetAllAsync(It.IsAny<EventFilterDto>()))
            .ReturnsAsync(new List<Event>());

        _eventRepoMock.Setup(r => r.CreateAsync(It.IsAny<Event>()))
            .ReturnsAsync(eventItem);

        // Act
        var result = await _eventService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Title, result.Title);
        Assert.Equal(createDto.TotalSeats, result.TotalSeats);
        Assert.Equal(createDto.TotalSeats, result.AvailableSeats);
    }

    [Fact]
    public async Task GetById_WithExistingId_ShouldReturnEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var eventItem = new Event("Test", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(34), 10);
        typeof(Event).GetProperty("Id")?.SetValue(eventItem, id);

        _eventRepoMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(eventItem);

        // Act
        var result = await _eventService.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingEvent = new Event("Old", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(34), 10);
        typeof(Event).GetProperty("Id")?.SetValue(existingEvent, id);

        var updateDto = TestDataGenerator.GetValidUpdateEventDto();

        _eventRepoMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(existingEvent);

        _eventRepoMock.Setup(r => r.GetAllAsync(It.IsAny<EventFilterDto>()))
            .ReturnsAsync(new List<Event>());

        _eventRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>()))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _eventService.UpdateAsync(id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(updateDto.Title, result.Title);
    }

    [Fact]
    public async Task GetAll_WithTitleFilter_ShouldReturnMatchingEvents()
    {
        // Arrange
        var filter = new EventFilterDto { Title = "expo" };
        var events = new List<Event>
        {
            new Event("HouseHold Expo", DateTime.UtcNow, DateTime.UtcNow.AddHours(4), 10),
            new Event("Composit expo", DateTime.UtcNow, DateTime.UtcNow.AddHours(4), 10)
        };

        _eventRepoMock.Setup(r => r.GetAllAsync(filter)).ReturnsAsync(events);

        // Act
        var result = await _eventService.GetAllAsync(filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Contains("expo", e.Title, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Неуспешные сценарии

    [Fact]
    public async Task GetById_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _eventRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _eventService.GetByIdAsync(id));
    }

    [Fact]
    public async Task Create_WithDuplicateTitle_ShouldThrowValidationException()
    {
        // Arrange
        var createDto = TestDataGenerator.GetValidCreateEventDTO();
        var existing = new Event(createDto.Title, DateTime.UtcNow, DateTime.UtcNow.AddHours(4), 10);

        _eventRepoMock.Setup(r => r.GetAllAsync(It.IsAny<EventFilterDto>()))
            .ReturnsAsync(new List<Event> { existing });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateAsync(createDto));
    }

    [Fact]
    public async Task Create_WithStartDateInPast_ShouldThrowValidationException()
    {
        // Arrange
        var createDto = new CreateEventDTO()
        {
            Title = "Past Event",
            Description = "Desc",
            StartAt = DateTime.UtcNow.AddDays(-1),
            EndAt = DateTime.UtcNow.AddDays(1),
            TotalSeats = 10
        };

        _eventRepoMock.Setup(r => r.GetAllAsync(It.IsAny<EventFilterDto>()))
            .ReturnsAsync(new List<Event>());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateAsync(createDto));
    }

    #endregion
}