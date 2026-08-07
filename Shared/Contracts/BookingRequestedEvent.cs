namespace EventManagement.Shared.Contracts;

/// <summary>
/// Событие запроса на бронирование (публикуется сервисом Bookings)
/// </summary>
public record BookingRequestedEvent(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime RequestedAt
);