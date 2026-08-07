namespace EventManagement.Shared.Contracts;

public record BookingCancelledEvent(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    DateTime CancelledAt
);