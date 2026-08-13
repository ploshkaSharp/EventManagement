using Moq;
using Xunit;
using EventManagement.Events.Application.Services;
using EventManagement.Events.Application.Ports;
using EventManagement.Events.Application.Constants;
using EventManagement.Events.Application.DTOs;
using EventManagement.Events.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EventManagement.Events.Application.Mappers;
using Microsoft.Extensions.DependencyInjection;

public class EventServiceCacheTests
{
    private readonly Mock<IEventRepository> _repoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly EventService _service;

    private readonly ServiceProvider _serviceProvider;  

    public EventServiceCacheTests()
    {
        _repoMock = new Mock<IEventRepository>();
        _cacheMock = new Mock<ICacheService>();
        var settings = Options.Create(new CacheSettings { Event = 600, Top10 = 300 });
        var logger = new Mock<ILogger<EventService>>().Object;
        _service = new EventService(_repoMock.Object, logger, _cacheMock.Object, settings);          
    } 

    [Fact]
    public async Task GetByIdAsync_CacheHit_DoesNotCallRepository()
    {
        // Arrange
        var @event = new Event("Title", DateTime.Now, DateTime.Now.AddDays(1));
        var id = @event.Id;
        var dto = EventMapper.ToDto(@event);
        _cacheMock.Setup(c => c.GetAsync<EventDTO>(CacheKeys.EventKey(id), default))
                  .ReturnsAsync(dto);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.Equal(dto, result);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_CallsRepositoryAndCaches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var eventItem = new Event("Title", DateTime.Now, DateTime.Now.AddHours(1), 10);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(eventItem);
        _cacheMock.Setup(c => c.GetAsync<EventDTO>(CacheKeys.EventKey(id), default))
                  .ReturnsAsync((EventDTO?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        _repoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(CacheKeys.EventKey(id), It.IsAny<EventDTO>(), It.IsAny<TimeSpan>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache()
    {
        // Arrange        
        var existing = new Event("Old", DateTime.Now, DateTime.Now.AddHours(1), 10);
        var id = existing.Id;
        var updateDto = new UpdateEventDTO(){Title = "New", Description = null, StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1)};                
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>())).ReturnsAsync(existing);

        // Act
        await _service.UpdateAsync(id, updateDto);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.EventKey(id), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.Top10Events, default), Times.Never);
    }

    [Fact]
    public async Task GetTop10Async_CacheHit_DoesNotCallRepository()
    {
        // Arrange
        var cacheKey = CacheKeys.Top10Events;
        var expectedEvents = new List<EventDTO>
        {
            new EventDTO()
            {
                Id = Guid.NewGuid(), 
                Title = "Event 1", 
                Description = null, 
                StartAt = DateTime.UtcNow, 
                EndAt = DateTime.UtcNow.AddHours(4), 
                TotalSeats = 100, 
                AvailableSeats = 10
            },
            new EventDTO()
            {
                Id = Guid.NewGuid(), 
                Title = "Event 2", 
                Description = null, 
                StartAt = DateTime.UtcNow, 
                EndAt = DateTime.UtcNow.AddHours(4), 
                TotalSeats = 100, 
                AvailableSeats = 20
            },
            new EventDTO()
            {
                Id = Guid.NewGuid(), 
                Title = "Event 3", 
                Description = null, 
                StartAt = DateTime.UtcNow, 
                EndAt = DateTime.UtcNow.AddHours(4), 
                TotalSeats = 100, 
                AvailableSeats = 30
            }
        };
        
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<EventDTO>>(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expectedEvents);

        // Act
        var result = await _service.GetTop10Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Equal(expectedEvents, result);
        
        // Репозиторий НЕ вызывается
        _repoMock.Verify(r => r.GetTop10ByPopularityAsync(), Times.Never);
        
        // Кеш читается один раз
        _cacheMock.Verify(c => c.GetAsync<IEnumerable<EventDTO>>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        
        // Кеш НЕ записывается
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTop10Async_CacheMiss_CallsRepositoryAndCaches()
    {
        // Arrange
        var cacheKey = CacheKeys.Top10Events;
        
        var events = new List<Event>
        {
            new Event("Event 1", DateTime.UtcNow, DateTime.UtcNow.AddHours(4), 100),
            new Event("Event 2", DateTime.UtcNow, DateTime.UtcNow.AddHours(4), 100),
            new Event("Event 3", DateTime.UtcNow, DateTime.UtcNow.AddHours(4), 100)
        };
        
        // Установка AvailableSeats для разных уровней популярности
        events[0].TryReserveSeats(90); // 90% занято
        events[1].TryReserveSeats(80); // 80% занято
        events[2].TryReserveSeats(70); // 70% занято
        
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<EventDTO>>(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((IEnumerable<EventDTO>?)null);
        
        _repoMock.Setup(r => r.GetTop10ByPopularityAsync()).ReturnsAsync(events);

        // Act
        var result = await _service.GetTop10Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        // Репозиторий вызывается один раз
        _repoMock.Verify(r => r.GetTop10ByPopularityAsync(), Times.Once);
        
        // Кеш читается один раз
        _cacheMock.Verify(c => c.GetAsync<IEnumerable<EventDTO>>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        
        // Кеш записывается с TTL для Top10
        _cacheMock.Verify(c => c.SetAsync(cacheKey, It.IsAny<IEnumerable<EventDTO>>(), TimeSpan.FromSeconds(300), It.IsAny<CancellationToken>()), Times.Once);
    }    
}