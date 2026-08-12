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
using EventManagement.Events.Application.Ports;

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
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.Top10Events, default), Times.Once);
    }
}