using EventManagement.Shared.Contracts;
using EventManagement.Events.Application.Ports;

namespace EventManagement.Events.Application.Handlers;

public interface IBookingConfirmedHandler
{
    Task HandleAsync(BookingConfirmedEvent @event);
}

public class BookingConfirmedHandler : IBookingConfirmedHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingConfirmedHandler> _logger;
    
    public BookingConfirmedHandler(IEventRepository eventRepository, ILogger<BookingConfirmedHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }
    
    public async Task HandleAsync(BookingConfirmedEvent @event)
    {
        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(@event.EventId);
            
            if (eventItem == null)
            {
                _logger.LogWarning("Event {EventId} not found for booking confirmation", @event.EventId);
                return;
            }
            
            if (!eventItem.TryReserveSeats(@event.SeatsCount))
            {
                _logger.LogWarning("Not enough seats for event {EventId}. Available: {Available}, Requested: {Requested}",
                    @event.EventId, eventItem.AvailableSeats, @event.SeatsCount);
                return;
            }
            
            await _eventRepository.UpdateAsync(eventItem);
            _logger.LogInformation("Successfully updated seats for event {EventId}. Booking {BookingId} confirmed",
                @event.EventId, @event.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BookingConfirmed event {BookingId}", @event.BookingId);
            throw;
        }
    }
}