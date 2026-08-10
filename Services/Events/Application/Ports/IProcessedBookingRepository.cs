using EventManagement.Events.Domain.Entities;

namespace EventManagement.Events.Application.Ports;

public interface IProcessedBookingRepository
{
    Task<bool> ExistsAsync(Guid bookingId);
    Task<bool> TryAddAsync(Guid bookingId, Guid eventId, Guid userId, DateTime processedAt);
    Task<ProcessedBookingResult?> GetResultAsync(Guid bookingId);
    Task UpdateResultAsync(Guid bookingId, bool success, string? failureReason, int availableSeats);    
}