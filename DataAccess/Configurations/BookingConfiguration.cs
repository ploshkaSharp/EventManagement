using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EventManagement.Models;

namespace EventManagement.Data.Configurations;

/// <summary>
/// Настройка таблицы Bookings
/// </summary>
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>
    /// Настройка таблицы
    /// </summary>
    /// <param name="builder">Объект-конфигуратор</param>
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .ValueGeneratedNever()
            .IsRequired();
        
        builder.Property(b => b.EventId)
            .IsRequired();
        
        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        
        builder.Property(b => b.CreatedAt)
            .IsRequired();
        
        builder.Property(b => b.ProcessedAt)
            .IsRequired(false);
        
        builder.HasIndex(b => b.EventId);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.CreatedAt);
        
        // Связь с Event (1:M)
        builder.HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}