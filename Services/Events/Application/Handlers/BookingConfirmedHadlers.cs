using EventManagement.Shared.Contracts;
using EventManagement.Events.Application.Ports;
using Microsoft.Extensions.Logging;

namespace EventManagement.Events.Application.Handlers;

public interface IBookingConfirmedHandler
{
    Task HandleAsync(BookingConfirmedEvent @event);
}

public class BookingConfirmedHandler : IBookingConfirmedHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IProcessedBookingRepository _processedBookingRepository;
    private readonly ILogger<BookingConfirmedHandler> _logger;
    
    public BookingConfirmedHandler(IEventRepository eventRepository, IProcessedBookingRepository processedBookingRepository, ILogger<BookingConfirmedHandler> logger)
    {
        _eventRepository = eventRepository;
        _processedBookingRepository = processedBookingRepository;
        _logger = logger;
    }
    
    public async Task HandleAsync(BookingConfirmedEvent @event)
    {
        try
        {
            // Проверить, что бронь уже была обработана
            var alreadyProcessed = await _processedBookingRepository.ExistsAsync(@event.BookingId);
            if (alreadyProcessed)
            {
                _logger.LogInformation("Booking {BookingId} already processed (detected in handler)", @event.BookingId);
                return;
            }

            var eventItem = await _eventRepository.GetByIdAsync(@event.EventId);
            
            if (eventItem == null)
            {
                _logger.LogWarning("Event {EventId} not found for booking confirmation", @event.EventId);

                // Создать запись об обработанной брони, даже если событие не найдено
                // чтобы не обрабатывать это сообщение повторно
                await _processedBookingRepository.AddAsync(@event.BookingId, @event.EventId, @event.UserId, DateTime.UtcNow);                
                return;
            }
            
            if (!eventItem.TryReserveSeats(@event.SeatsCount))
            {
                _logger.LogWarning("Not enough seats for event {EventId}. Available: {Available}, Requested: {Requested}",
                    @event.EventId, eventItem.AvailableSeats, @event.SeatsCount);

                // Записать как обработанное, чтобы не повторять ошибку
                await _processedBookingRepository.AddAsync(@event.BookingId, @event.EventId, @event.UserId, DateTime.UtcNow);                    
                return;
            }
            
            await _eventRepository.UpdateAsync(eventItem);
            _logger.LogInformation("Successfully updated seats for event {EventId}. Booking {BookingId} confirmed",
                @event.EventId, @event.BookingId);
            
            // Сохранить запись об обработанной брони
            await _processedBookingRepository.AddAsync(@event.BookingId, @event.EventId, @event.UserId, DateTime.UtcNow);

            _logger.LogInformation("Successfully processed booking {BookingId} for event {EventId}. Available seats: {Available}",
                                    @event.BookingId, @event.EventId, eventItem.AvailableSeats);            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BookingConfirmed event {BookingId}", @event.BookingId);
            throw;
        }
    }
}