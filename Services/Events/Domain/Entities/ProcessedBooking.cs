namespace EventManagement.Events.Domain.Entities;

/// <summary>
/// Сущность для хранения обработанных броней (идемпотентность)
/// </summary>
public class ProcessedBooking
{
    private ProcessedBooking() { }

    public ProcessedBooking(Guid bookingId, Guid eventId, Guid userId, DateTime processedAt)
    {
        Id = Guid.NewGuid();
        BookingId = bookingId;
        EventId = eventId;
        UserId = userId;
        ProcessedAt = processedAt;
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime ProcessedAt { get; private set; }
}