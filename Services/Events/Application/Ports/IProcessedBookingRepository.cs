namespace EventManagement.Events.Application.Ports;

public interface IProcessedBookingRepository
{
    Task<bool> ExistsAsync(Guid bookingId);
    Task AddAsync(Guid bookingId, Guid eventId, Guid userId, DateTime processedAt);
}