namespace EventManagement.Shared.Contracts;

/// <summary>
/// Контракт события
/// </summary>
/// <param name="BookingId"></param>
/// <param name="EventId"></param>
/// <param name="UserId"></param>
/// <param name="SeatsCount"></param>
/// <param name="ConfirmedAt"></param>
public record BookingConfirmedEvent(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime ConfirmedAt
);