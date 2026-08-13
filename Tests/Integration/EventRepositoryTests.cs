using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Events.Application.Ports;
using EventManagement.Events.Application.DTOs;
using EventManagement.Events.Domain.Entities;
using EventManagement.Events.Infrastructure.Data;
using EventManagement.Events.Infrastructure.Repositories;

namespace EventManagement.IntegrationTests.Repositories;

/// <summary>
/// Базовый класс для интеграционных тестов репозитория событий
/// </summary>
public abstract class EventIntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer _postgresContainer;
    protected ServiceProvider _serviceProvider;
    protected string _connectionString;

    protected EventIntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("eventmanagement_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    protected async Task ResetDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}

/// <summary>
/// Интеграционные тесты репозитория мероприятий
/// </summary>
public class EventRepositoryTests : EventIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_ShouldAddEventToDatabase()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);

        var result = await repository.CreateAsync(eventItem);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(eventItem.Title, result.Title);
        Assert.Equal(eventItem.TotalSeats, result.TotalSeats);
        Assert.Equal(eventItem.AvailableSeats, result.AvailableSeats);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
        var created = await repository.CreateAsync(eventItem);

        var result = await repository.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_WithoutFilters_ShouldReturnAllEvents()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var events = new[]
        {
            new Event("Event 1", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(4), 10),
            new Event("Event 2", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(4), 20),
            new Event("Event 3", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(4), 30)
        };

        foreach (var e in events)
            await repository.CreateAsync(e);

        var result = await repository.GetAllAsync();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithTitleFilter_ShouldReturnMatchingEvents()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var events = new[]
        {
            new Event("Tech Conference", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(4), 10),
            new Event("Auto Conference", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(4), 20),
            new Event("Tech Workshop", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(4), 30)
        };

        foreach (var e in events)
            await repository.CreateAsync(e);

        var filter = new EventFilterDto { Title = "tech conference" };

        var result = await repository.GetAllAsync(filter);

        Assert.Single(result);
        Assert.All(result, e => Assert.Contains("Tech", e.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_WithDateRangeFilter_ShouldReturnEventsInRange()
    {
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
            await repository.CreateAsync(e);

        var filter = new EventFilterDto { From = now.AddDays(2), To = now.AddDays(8) };

        var result = await repository.GetAllAsync(filter);

        Assert.Single(result);
        Assert.Equal("Event 2", result.First().Title);
    }

    [Fact]
    public async Task GetPaginatedAsync_ShouldReturnCorrectPage()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        for (int i = 1; i <= 25; i++)
        {
            var eventItem = new Event($"Event {i}", DateTime.UtcNow.AddDays(i), DateTime.UtcNow.AddDays(i).AddHours(4), 10);
            await repository.CreateAsync(eventItem);
        }

        var filter = new EventFilterDto { PageNumber = 2, PageSize = 10 };

        var result = await repository.GetPaginatedAsync(filter);

        Assert.Equal(10, result.Items.Count());
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task TryReserveSeatsAsync_WithAvailableSeats_ShouldSucceed()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
        var created = await repository.CreateAsync(eventItem);

        var result = await repository.TryReserveSeatsAsync(created.Id, 3);

        Assert.True(result);
        var updated = await repository.GetByIdAsync(created.Id);
        Assert.Equal(7, updated?.AvailableSeats);
    }

    [Fact]
    public async Task TryReserveSeatsAsync_WithInsufficientSeats_ShouldFail()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 5);
        var created = await repository.CreateAsync(eventItem);

        var result = await repository.TryReserveSeatsAsync(created.Id, 10);

        Assert.False(result);
        var updated = await repository.GetByIdAsync(created.Id);
        Assert.Equal(5, updated?.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseSeatsAsync_ShouldIncreaseAvailableSeats()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
        var created = await repository.CreateAsync(eventItem);
        await repository.TryReserveSeatsAsync(created.Id, 3);

        await repository.ReleaseSeatsAsync(created.Id, 2);

        var updated = await repository.GetByIdAsync(created.Id);
        Assert.Equal(9, updated?.AvailableSeats);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventItem = new Event("Original Title", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
        var created = await repository.CreateAsync(eventItem);

        created.Title = "Updated Title";
        created.Description = "Updated Description";

        var result = await repository.UpdateAsync(created);

        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated Description", result.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventItem = new Event("Test Event", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4), 10);
        var created = await repository.CreateAsync(eventItem);

        var result = await repository.DeleteAsync(created.Id);

        Assert.True(result);
        var deleted = await repository.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }
}