using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Data;
using EventManagement.Repositories;
using EventManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.IntegrationTests.Base;

/// <summary>
/// Интеграционные тесты
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
  /// <summary>
  /// Контейнер для PostgreSql
  /// </summary>
  protected readonly PostgreSqlContainer _postgresContainer;
  /// <summary>
  /// Провайдер
  /// </summary>
  protected ServiceProvider _serviceProvider;
  /// <summary>
  /// Строка подключения к sql
  /// </summary>
  protected string _connectionString;
  /// <summary>
  /// Создать контейнер с PostgreSql
  /// </summary>
  protected IntegrationTestBase()
  {
    _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventmanagement_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();
  }

  /// <summary>
  /// Инициализация интегр. тестов
  /// </summary>
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

  /// <summary>
  /// Освободить ресурсы
  /// </summary>
  public virtual async Task DisposeAsync()
  {
    await _postgresContainer.DisposeAsync();
    await _serviceProvider.DisposeAsync();
  }

  /// <summary>
  /// Очистить БД
  /// </summary>
  /// <returns></returns>
  protected async Task ResetDatabaseAsync()
  {
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();
  }
}