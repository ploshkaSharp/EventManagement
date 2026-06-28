using Xunit;
using EventManagement.Application.Ports;
using EventManagement.Application.DTOs;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Repositories;
using EventManagement.IntegrationTests.Base;
using Microsoft.Extensions.DependencyInjection;

namespace EventManagement.IntegrationTests.Repositories;

/// <summary>
/// Интеграционные тесты мероприятий
/// </summary>
public class EventRepositoryTests : IntegrationTestBase
{
  /// <summary>
  /// Создание
  /// </summary>  
  [Fact]
  public async Task CreateAsync_ShouldAddEventToDatabase()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);

    // Act
    var result = await repository.CreateAsync(eventItem);

    // Assert
    Assert.NotNull(result);
    Assert.NotEqual(Guid.Empty, result.Id);
    Assert.Equal(eventItem.Title, result.Title);
    Assert.Equal(eventItem.TotalSeats, result.TotalSeats);
    Assert.Equal(eventItem.AvailableSeats, result.AvailableSeats);
  }

  /// <summary>
  /// Получить по ИД
  /// </summary>
  [Fact]
  public async Task GetByIdAsync_WithExistingId_ShouldReturnEvent()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var created = await repository.CreateAsync(eventItem);

    // Act
    var result = await repository.GetByIdAsync(created.Id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(created.Id, result.Id);
    Assert.Equal(created.Title, result.Title);
  }

  /// <summary>
  /// Получить по несуществующему ИД
  /// </summary>
  [Fact]
  public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

    // Act
    var result = await repository.GetByIdAsync(Guid.NewGuid());

    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Получить все
  /// </summary>  
  [Fact]
  public async Task GetAllAsync_WithoutFilters_ShouldReturnAllEvents()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

    var events = new[]
    {
            new Event("Event 1", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(4), 10),
            new Event("Event 2", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1).AddHours(4), 20),
            new Event("Event 3", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(1).AddHours(4), 30)
        };

    foreach (var e in events)
    {
      await repository.CreateAsync(e);
    }

    // Act
    var result = await repository.GetAllAsync();

    // Assert
    Assert.Equal(3, result.Count());
  }

  /// <summary>
  /// Получить все (с фильтром по названию)
  /// </summary>
  [Fact]
  public async Task GetAllAsync_WithTitleFilter_ShouldReturnMatchingEvents()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

    var events = new[]
    {
            new Event("Tech Conference", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(4), 10),
            new Event("Auto Conference", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1).AddHours(4), 20),
            new Event("Tech Workshop", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(1).AddHours(4), 30)
        };

    foreach (var e in events)
    {
      await repository.CreateAsync(e);
    }

    var filter = new EventFilterDto { Title = "tech conference" };

    // Act
    var result = await repository.GetAllAsync(filter);

    // Assert
    Assert.Single(result);
    Assert.All(result, e => Assert.Contains("Tech", e.Title, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Получить (с фильтром по периоду)
  /// </summary>  
  [Fact]
  public async Task GetAllAsync_WithDateRangeFilter_ShouldReturnEventsInRange()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

    var now = DateTime.UtcNow;
    var events = new[]
    {
            new Event("Event 1", now.AddDays(1), now.AddDays(1).AddHours(4), 10),
            new Event("Event 2", now.AddDays(5), now.AddDays(5).AddHours(4), 20),
            new Event("Event 3", now.AddDays(10), now.AddDays(10).AddHours(4), 30)
    };

    foreach (var e in events)
    {
      await repository.CreateAsync(e);
    }

    var filter = new EventFilterDto
    {
      From = now.AddDays(2),
      To = now.AddDays(8)
    };

    // Act
    var result = await repository.GetAllAsync(filter);

    // Assert
    Assert.Single(result);
    Assert.Equal("Event 2", result.First().Title);
  }

  /// <summary>
  /// Получить пагинрованный результат
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task GetPaginatedAsync_ShouldReturnCorrectPage()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

    for (int i = 1; i <= 25; i++)
    {
      var eventItem = new Event($"Event {i}", DateTime.UtcNow.AddDays(i), DateTime.UtcNow.AddDays(i).AddHours(4), 10);
      await repository.CreateAsync(eventItem);
    }

    var filter = new EventFilterDto { PageNumber = 2, PageSize = 10 };

    // Act
    var result = await repository.GetPaginatedAsync(filter);

    // Assert
    Assert.Equal(10, result.Items.Count());
    Assert.Equal(25, result.TotalCount);
    Assert.Equal(2, result.PageNumber);
    Assert.Equal(10, result.PageSize);
    Assert.Equal(3, result.TotalPages);
    Assert.True(result.HasPreviousPage);
    Assert.True(result.HasNextPage);
  }

  /// <summary>
  /// Забронировать места
  /// </summary>  
  [Fact]
  public async Task TryReserveSeatsAsync_WithAvailableSeats_ShouldSucceed()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var created = await repository.CreateAsync(eventItem);

    // Act
    var result = await repository.TryReserveSeatsAsync(created.Id, 3);

    // Assert
    Assert.True(result);
    var updated = await repository.GetByIdAsync(created.Id);
    Assert.Equal(7, updated?.AvailableSeats);
  }

  /// <summary>
  /// Забронировать больше чем есть
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task TryReserveSeatsAsync_WithInsufficientSeats_ShouldFail()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 5);
    var created = await repository.CreateAsync(eventItem);

    // Act
    var result = await repository.TryReserveSeatsAsync(created.Id, 10);

    // Assert
    Assert.False(result);
    var updated = await repository.GetByIdAsync(created.Id);
    Assert.Equal(5, updated?.AvailableSeats);
  }

  /// <summary>
  /// Отменить бронь на места
  /// </summary>
  [Fact]
  public async Task ReleaseSeatsAsync_ShouldIncreaseAvailableSeats()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var created = await repository.CreateAsync(eventItem);
    await repository.TryReserveSeatsAsync(created.Id, 3);

    // Act
    await repository.ReleaseSeatsAsync(created.Id, 2);

    // Assert
    var updated = await repository.GetByIdAsync(created.Id);
    Assert.Equal(9, updated?.AvailableSeats);
  }

  /// <summary>
  /// Обновить
  /// </summary>  
  [Fact]
  public async Task UpdateAsync_ShouldModifyEvent()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var eventItem = new Event("Original Title", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var created = await repository.CreateAsync(eventItem);

    created.Title = "Updated Title";
    created.Description = "Updated Description";

    // Act
    var result = await repository.UpdateAsync(created);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Updated Title", result.Title);
    Assert.Equal("Updated Description", result.Description);
  }

  /// <summary>
  /// Удалить
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task DeleteAsync_ShouldRemoveEvent()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
    var created = await repository.CreateAsync(eventItem);

    // Act
    var result = await repository.DeleteAsync(created.Id);

    // Assert
    Assert.True(result);
    var deleted = await repository.GetByIdAsync(created.Id);
    Assert.Null(deleted);
  }
}