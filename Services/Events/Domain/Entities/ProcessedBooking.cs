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
        Success = false;
        FailureReason = null;
        AvailableSeats = 0;        
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public bool Success { get; private set; }
    public string? FailureReason { get; private set; }
    public int AvailableSeats { get; private set; } 

    public void SetResult(bool success, string? failureReason, int availableSeats)
    {
        Success = success;
        FailureReason = failureReason;
        AvailableSeats = availableSeats;
    }       
}

/// <summary>
/// DTO для результата обработки брони
/// </summary>
public record ProcessedBookingResult(
    Guid BookingId,
    bool Success,
    string? FailureReason,
    int AvailableSeats,
    DateTime ProcessedAt
);