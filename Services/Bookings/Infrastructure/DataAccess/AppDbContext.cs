using Microsoft.EntityFrameworkCore;
using EventManagement.Bookings.Domain.Entities;

namespace EventManagement.Bookings.Infrastructure.Data;

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
    /// Таблица Bookings в БД
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// Переопредление пользовательской настройкой модели БД
    /// </summary>
    /// <param name="modelBuilder">Объект построения модели</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}