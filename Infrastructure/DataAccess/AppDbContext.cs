using Microsoft.EntityFrameworkCore;
using EventManagement.Domain.Entities;

namespace EventManagement.Infrastructure.Data;

/// <summary>
/// 
/// </summary>
public sealed class AppDbContext : DbContext
{
  /// <summary>
  /// Создать пользовательский контекст БД
  /// </summary>
  /// <param name="options"></param>
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  /// <summary>
  /// Таблица Events в БД
  /// </summary>
  public DbSet<Event> Events => Set<Event>();

  /// <summary>
  /// Таблица Bookings в БД
  /// </summary>
  public DbSet<Booking> Bookings => Set<Booking>();

  /// <summary>
  /// Таблица Users в БД
  /// </summary>
  public DbSet<User> Users => Set<User>();

  /// <summary>
  /// Переопредление пользовательской настройкой модели БД
  /// </summary>
  /// <param name="modelBuilder">Объект построения модели</param>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  }
}