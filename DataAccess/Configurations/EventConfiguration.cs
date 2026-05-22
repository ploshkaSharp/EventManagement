using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EventManagement.Models;

namespace EventManagement.Data.Configurations;

/// <summary>
/// Настройка таблицы Events
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <summary>
    /// Настройка таблицы
    /// </summary>
    /// <param name="builder">Объект-конфигуратор</param>
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .IsRequired();
        
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(e => e.Description)
            .HasMaxLength(1000);
        
        builder.Property(e => e.StartAt)
            .IsRequired();
        
        builder.Property(e => e.EndAt)
            .IsRequired();
        
        builder.Property(e => e.TotalSeats)
            .IsRequired();
        
        builder.Property(e => e.AvailableSeats)
            .IsRequired();
        
        builder.HasIndex(e => e.StartAt);
        builder.HasIndex(e => e.Title);
        
        // Связь с Booking (1:M)
        builder.HasMany(e => e.Bookings)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}