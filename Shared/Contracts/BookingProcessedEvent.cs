namespace EventManagement.Shared.Contracts;

/// <summary>
/// Событие результата обработки бронирования (публикуется сервисом Events)
/// </summary>
public record BookingProcessedEvent(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    bool Success,
    string? FailureReason,
    DateTime ProcessedAt,
    int AvailableSeats
);