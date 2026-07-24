using EventManagement.Shared.Contracts;

namespace EventManagement.Bookings.Application.Ports;

public interface IEventPublisher
{
    Task PublishBookingConfirmedAsync(BookingConfirmedEvent @event);
}