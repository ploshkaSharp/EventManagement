using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EventManagement.Events.Domain.Entities;

namespace EventManagement.Events.Infrastructure.Configurations;

public class ProcessedBookingConfiguration : IEntityTypeConfiguration<ProcessedBooking>
{
    public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
    {
        builder.ToTable("ProcessedBookings");
        
        builder.HasKey(pb => pb.Id);
        
        builder.Property(pb => pb.BookingId)
            .IsRequired()
            .HasColumnName("BookingId");
        
        builder.Property(pb => pb.EventId)
            .IsRequired()
            .HasColumnName("EventId");
        
        builder.Property(pb => pb.UserId)
            .IsRequired()
            .HasColumnName("UserId");
        
        builder.Property(pb => pb.ProcessedAt)
            .IsRequired()
            .HasColumnName("ProcessedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Уникальный индекс для идемпотентности
        builder.HasIndex(pb => pb.BookingId)
            .IsUnique()
            .HasDatabaseName("IX_ProcessedBookings_BookingId");
    }
}