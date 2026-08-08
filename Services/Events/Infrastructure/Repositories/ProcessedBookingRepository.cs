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

    public async Task<bool> TryAddAsync(Guid bookingId, Guid eventId, Guid userId, DateTime processedAt)
    {
        try
        {
            var affected = await _context.Database.ExecuteSqlRawAsync(
                @"
                    INSERT INTO ""ProcessedBookings"" (""Id"", ""BookingId"", ""EventId"", ""UserId"", ""ProcessedAt"", ""Success"", ""FailureReason"", ""AvailableSeats"")
                    VALUES (gen_random_uuid(), {0}, {1}, {2}, {3}, false, NULL, 0)
                    ON CONFLICT (""BookingId"") DO NOTHING
                ",
                bookingId,
                eventId,
                userId,
                processedAt);

            var inserted = affected == 1;

            if (inserted)
            {
                _logger.LogDebug("Successfully inserted ProcessedBooking record for booking {BookingId}", bookingId);
            }
            else
            {
                _logger.LogDebug("ProcessedBooking record for booking {BookingId} already exists", bookingId);
            }

            return inserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding booking {BookingId} to ProcessedBookings", bookingId);
            throw;
        }
    }

    /// <summary>
    /// Получить результат обработки брони
    /// </summary>
    public async Task<ProcessedBookingResult?> GetResultAsync(Guid bookingId)
    {
        var record = await _context.ProcessedBookings
            .Where(pb => pb.BookingId == bookingId)
            .Select(pb => new ProcessedBookingResult(
                pb.BookingId,
                pb.Success,
                pb.FailureReason,
                pb.AvailableSeats,
                pb.ProcessedAt))
            .FirstOrDefaultAsync();

        if (record != null)
        {
            _logger.LogDebug("Retrieved result for booking {BookingId}: Success={Success}", bookingId, record.Success);
        }

        return record;
    }

    /// <summary>
    /// Обновить результат обработки брони
    /// </summary>
    public async Task UpdateResultAsync(Guid bookingId, bool success, string? failureReason, int availableSeats)
    {
        try
        {
            var affected = await _context.Database.ExecuteSqlRawAsync(
                @"
                    UPDATE ""ProcessedBookings""
                    SET ""Success"" = {0},
                        ""FailureReason"" = {1},
                        ""AvailableSeats"" = {2}
                    WHERE ""BookingId"" = {3}
                ",
                success,
                failureReason,
                availableSeats,
                bookingId);

            if (affected == 0)
            {
                _logger.LogWarning("No ProcessedBooking record found to update for booking {BookingId}", bookingId);
            }
            else
            {
                _logger.LogDebug("Updated ProcessedBooking record for booking {BookingId}: Success={Success}", bookingId, success);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ProcessedBooking record for booking {BookingId}", bookingId);
            throw;
        }
    }    
}