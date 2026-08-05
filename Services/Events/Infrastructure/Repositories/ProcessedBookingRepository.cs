using Microsoft.EntityFrameworkCore;
using EventManagement.Events.Application.Ports;
using EventManagement.Events.Domain.Entities;
using EventManagement.Events.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace EventManagement.Events.Infrastructure.Repositories;

public class ProcessedBookingRepository : IProcessedBookingRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProcessedBookingRepository> _logger;

    public ProcessedBookingRepository(AppDbContext context, ILogger<ProcessedBookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(Guid bookingId)
    {
        return await _context.ProcessedBookings.AnyAsync(pb => pb.BookingId == bookingId);
    }

    public async Task AddAsync(Guid bookingId, Guid eventId, Guid userId, DateTime processedAt)
    {
        try
        {            
            var affected = await _context.Database.ExecuteSqlRawAsync(
                @"
                    INSERT INTO ""ProcessedBookings"" (""Id"", ""BookingId"", ""EventId"", ""UserId"", ""ProcessedAt"")
                    VALUES (gen_random_uuid(), {0}, {1}, {2}, {3})
                    ON CONFLICT (""BookingId"") DO NOTHING
                ",
                bookingId,
                eventId,
                userId,
                processedAt);

            if (affected == 0)
            {
                _logger.LogDebug("Booking {BookingId} already exists in ProcessedBookings", bookingId);
            }
            else
            {
                _logger.LogDebug("Booking {BookingId} added to ProcessedBookings", bookingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding booking {BookingId} to ProcessedBookings", bookingId);
            throw;
        }
    }
}