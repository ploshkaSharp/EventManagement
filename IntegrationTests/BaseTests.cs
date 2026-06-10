using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Data;
using EventManagement.Repositories;
using EventManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.IntegrationTests.Base;

public abstract class IntegrationTestBase : IAsyncLifetime
{  
  protected readonly PostgreSqlContainer _postgresContainer;
  protected ServiceProvider _serviceProvider;
  protected string _connectionString;

  protected IntegrationTestBase()
  {
    _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventmanagement_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();
  }

  public virtual async Task InitializeAsync()
  {
    await _postgresContainer.StartAsync();
    _connectionString = _postgresContainer.GetConnectionString();

    var services = new ServiceCollection();

    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));

    services.AddScoped<IEventRepository, EventRepository>();
    services.AddScoped<IBookingRepository, BookingRepository>();
    services.AddScoped<IEventService, EventService>();
    services.AddScoped<IBookingService, BookingService>();
    services.AddLogging();

    _serviceProvider = services.BuildServiceProvider();

    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
  }

  public virtual async Task DisposeAsync()
  {
    await _postgresContainer.DisposeAsync();
    await _serviceProvider.DisposeAsync();
    /*
    if (_serviceProvider is IAsyncDisposable asyncDisposable)
    {
      await asyncDisposable.DisposeAsync();
    }
    else
    {
      _serviceProvider?.Dispose();
    }
    */
  }

  protected async Task ResetDatabaseAsync()
  {
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();
  }
}