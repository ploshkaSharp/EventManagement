using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Domain.Entities;
using EventManagement.Bookings.Domain.Enums;
using EventManagement.Bookings.Infrastructure.Data;
using EventManagement.Bookings.Infrastructure.Repositories;

namespace EventManagement.IntegrationTests.Repositories;

/// <summary>
/// Базовый класс для интеграционных тестов репозитория бронирований
/// </summary>
public abstract class BookingIntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer _postgresContainer;
    protected ServiceProvider _serviceProvider;
    protected string _connectionString;

    protected BookingIntegrationTestBase()
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
        services.AddScoped<IBookingRepository, BookingRepository>();
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
/// Интеграционные тесты репозитория бронирований
/// </summary>
public class BookingRepositoryTests : BookingIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_ShouldAddBookingToDatabase()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var result = await bookingRepository.CreateAsync(booking);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnBooking()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var created = await bookingRepository.CreateAsync(booking);

        var result = await bookingRepository.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
    }

    [Fact]
    public async Task GetByEventIdAsync_ShouldReturnBookingsForSpecificEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var eventId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            var booking = new Booking(eventId, Guid.NewGuid());
            await bookingRepository.CreateAsync(booking);
        }

        var result = await bookingRepository.GetByEventIdAsync(eventId);

        Assert.Equal(5, result.Count());
        Assert.All(result, b => Assert.Equal(eventId, b.EventId));
    }

    [Fact]
    public async Task GetPendingBookingsAsync_ShouldReturnOnlyPendingBookings()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking1 = new Booking(eventId, userId);
        var booking2 = new Booking(eventId, userId);
        var booking3 = new Booking(eventId, userId);

        var created1 = await bookingRepository.CreateAsync(booking1);
        var created2 = await bookingRepository.CreateAsync(booking2);
        var created3 = await bookingRepository.CreateAsync(booking3);

        created2.Confirm();
        await bookingRepository.UpdateAsync(created2);

        var result = await bookingRepository.GetBookingByStatusAsync(BookingStatus.Pending);

        Assert.Equal(2, result.Count());
        Assert.Contains(result, b => b.Id == created1.Id);
        Assert.Contains(result, b => b.Id == created3.Id);
        Assert.DoesNotContain(result, b => b.Id == created2.Id);
        Assert.All(result, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyBookingStatus()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var created = await bookingRepository.CreateAsync(booking);

        created.Confirm();

        var result = await bookingRepository.UpdateAsync(created);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveBooking()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var created = await bookingRepository.CreateAsync(booking);

        var result = await bookingRepository.DeleteAsync(created.Id);

        Assert.True(result);
        var deleted = await bookingRepository.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }
}