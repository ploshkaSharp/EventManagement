using EventManagement.Shared.Contracts;
using EventManagement.Shared.Topics;
using EventManagement.Events.Application.Ports;
using Microsoft.Extensions.Logging;

namespace EventManagement.Events.Application.Handlers;

/// <summary>
/// Обработчик запросов на бронирование
/// </summary>
public interface IBookingRequestedHandler
{
    Task HandleAsync(EventManagement.Shared.Contracts.BookingRequestedEvent @event);
}

/// <summary>
/// Реализация обработчика запросов на бронирование
/// </summary>
public class BookingRequestedHandler : IBookingRequestedHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IProcessedBookingRepository _processedBookingRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<BookingRequestedHandler> _logger;

    public BookingRequestedHandler(
        IEventRepository eventRepository,
        IProcessedBookingRepository processedBookingRepository,
        IEventPublisher eventPublisher,
        ILogger<BookingRequestedHandler> logger)
    {
        _eventRepository = eventRepository;
        _processedBookingRepository = processedBookingRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(EventManagement.Shared.Contracts.BookingRequestedEvent @event)
    {
        _logger.LogInformation(
            "Processing booking request {BookingId} for event {EventId} by user {UserId}",
            @event.BookingId,
            @event.EventId,
            @event.UserId);

        try
        {
            // 1. Проверить, не было ли это бронирование уже обработано (идемпотентность)
            var alreadyProcessed = await _processedBookingRepository.ExistsAsync(@event.BookingId);
            if (alreadyProcessed)
            {
                _logger.LogInformation(
                    "Booking {BookingId} already processed, sending duplicate response",
                    @event.BookingId);
                
                // Отправить результат с признаком уже обработано
                await SendProcessedEvent(@event, true, "Already processed", 0);
                return;
            }

            // 2. Получить мероприятие
            var eventItem = await _eventRepository.GetByIdAsync(@event.EventId);
            
            if (eventItem == null)
            {
                _logger.LogWarning(
                    "Event {EventId} not found for booking {BookingId}",
                    @event.EventId,
                    @event.BookingId);
                
                // Сохранить как обработанное, чтобы не обрабатывать повторно
                await _processedBookingRepository.AddAsync(
                    @event.BookingId,
                    @event.EventId,
                    @event.UserId,
                    DateTime.UtcNow);
                
                // Отправить результат с ошибкой
                await SendProcessedEvent(
                    @event, 
                    false, 
                    $"Event {@event.EventId} not found",
                    0);
                return;
            }

            // 3. Проверить, активно ли мероприятие
            if (eventItem.StartAt < DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Event {EventId} is not active (started at {StartAt}) for booking {BookingId}",
                    @event.EventId,
                    eventItem.StartAt,
                    @event.BookingId);
                
                await _processedBookingRepository.AddAsync(
                    @event.BookingId,
                    @event.EventId,
                    @event.UserId,
                    DateTime.UtcNow);
                
                await SendProcessedEvent(
                    @event, 
                    false, 
                    $"Event has already started at {eventItem.StartAt:yyyy-MM-dd HH:mm:ss} UTC",
                    eventItem.AvailableSeats);
                return;
            }

            // 4. Проверить наличие достаточного количества мест
            var availableBefore = eventItem.AvailableSeats;
            if (!eventItem.TryReserveSeats(@event.SeatsCount))
            {
                _logger.LogWarning(
                    "Not enough seats for event {EventId}. Available: {Available}, Requested: {Requested}",
                    @event.EventId,
                    eventItem.AvailableSeats,
                    @event.SeatsCount);
                
                await _processedBookingRepository.AddAsync(
                    @event.BookingId,
                    @event.EventId,
                    @event.UserId,
                    DateTime.UtcNow);
                
                await SendProcessedEvent(
                    @event, 
                    false, 
                    $"Not enough seats. Available: {availableBefore}, Requested: {@event.SeatsCount}",
                    availableBefore);
                return;
            }

            // 5. Обновить мероприятие в базе данных
            await _eventRepository.UpdateAsync(eventItem);
            
            // 6. Сохранить запись об обработанной брони
            await _processedBookingRepository.AddAsync(
                @event.BookingId,
                @event.EventId,
                @event.UserId,
                DateTime.UtcNow);

            // 7. Отправить успешный результат
            await SendProcessedEvent(
                @event, 
                true, 
                null, 
                eventItem.AvailableSeats);
            
            _logger.LogInformation(
                "Successfully processed booking {BookingId} for event {EventId}. " +
                "Seats: {AvailableBefore} -> {AvailableAfter}",
                @event.BookingId,
                @event.EventId,
                availableBefore,
                eventItem.AvailableSeats);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing booking request {BookingId} for event {EventId}",
                @event.BookingId,
                @event.EventId);
            
            // В случае ошибки отправить негативный результат
            try
            {
                await SendProcessedEvent(
                    @event, 
                    false, 
                    $"Internal error: {ex.Message}",
                    0);
            }
            catch (Exception sendEx)
            {
                _logger.LogError(
                    sendEx,
                    "Failed to send processed event for booking {BookingId}",
                    @event.BookingId);
            }
            
            throw;
        }
    }

    /// <summary>
    /// Отправка результата обработки бронирования
    /// </summary>
    private async Task SendProcessedEvent(
        EventManagement.Shared.Contracts.BookingRequestedEvent @event, 
        bool success, 
        string? failureReason, 
        int availableSeats)
    {
        var processedEvent = new BookingProcessedEvent(
            @event.BookingId,
            @event.EventId,
            @event.UserId,
            success,
            failureReason,
            DateTime.UtcNow,
            availableSeats
        );

        await _eventPublisher.PublishAsync(
            KafkaTopics.BookingProcessed,
            @event.EventId.ToString(),
            processedEvent);
        
        _logger.LogInformation(
            "Sent BookingProcessed event for booking {BookingId}, Success: {Success}, Reason: {Reason}",
            @event.BookingId,
            success,
            failureReason ?? "none");
    }
}