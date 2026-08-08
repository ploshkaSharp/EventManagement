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

        // Результат обработки
        bool success = false;
        string? failureReason = null;
        int availableSeats = 0;

        try
        {
            // Попытататься вставить запись первой с уникальным ключом
            // Только один экземпляр сможет выполнить INSERT, остальные получат affected = 0
            var inserted = await _processedBookingRepository.TryAddAsync(
                @event.BookingId,
                @event.EventId,
                @event.UserId,
                DateTime.UtcNow);

            if (!inserted)
            {
                // Если запись уже существует, получится сохраненный результат
                _logger.LogInformation("Booking {BookingId} already processed, retrieving stored result", @event.BookingId);

                var storedResult = await _processedBookingRepository.GetResultAsync(@event.BookingId);
                if (storedResult != null)
                {
                    // Отправить сохраненный результат (повтор)
                    await SendProcessedEvent(@event, storedResult.Success, storedResult.FailureReason, storedResult.AvailableSeats);
                }
                else
                {
                    // Если результат не найден, отправляем по-умолчанию
                    await SendProcessedEvent(@event, false, "Unknown processing state", 0);
                }
                return;
            }

            try
            {

                // 1. Получить мероприятие
                var eventItem = await _eventRepository.GetByIdAsync(@event.EventId);

                if (eventItem == null)
                {
                    failureReason = $"Event {@event.EventId} not found";
                    _logger.LogWarning(failureReason, @event.EventId, @event.BookingId);
                    success = false;
                    availableSeats = 0;
                    await _processedBookingRepository.UpdateResultAsync(
                        @event.BookingId,
                        success,
                        failureReason,
                        availableSeats);
                    return;
                }

                // 3. Проверить, активно ли мероприятие
                if (eventItem.StartAt < DateTime.UtcNow)
                {
                    failureReason = $"Event has already started at {eventItem.StartAt:yyyy-MM-dd HH:mm:ss}";
                    _logger.LogWarning("Event {EventId} is not active for booking {BookingId}", @event.EventId, @event.BookingId);

                    success = false;
                    availableSeats = eventItem.AvailableSeats;

                    await _processedBookingRepository.UpdateResultAsync(
                        @event.BookingId,
                        success,
                        failureReason,
                        availableSeats);
                    return;
                }

                // 4. Проверить наличие достаточного количества мест
                var availableBefore = eventItem.AvailableSeats;
                if (!eventItem.TryReserveSeats(@event.SeatsCount))
                {
                    failureReason = $"Not enough seats. Available: {availableBefore}, Requested: {@event.SeatsCount}";
                    _logger.LogWarning("Not enough seats for event {EventId}. Available: {Available}, Requested: {Requested}",
                        @event.EventId,
                        eventItem.AvailableSeats,
                        @event.SeatsCount);

                    success = false;
                    availableSeats = availableBefore;

                    await _processedBookingRepository.UpdateResultAsync(
                        @event.BookingId,
                        success,
                        failureReason,
                        availableSeats);
                    return;
                }

                // Успешно зарезервировано места 
                success = true;
                availableSeats = eventItem.AvailableSeats;

                // 5. Обновить запись об обработанной брони с результатом
                await _processedBookingRepository.UpdateResultAsync(
                    @event.BookingId,
                    success,
                    null,
                    availableSeats);

                _logger.LogInformation(
                    "Successfully processed booking {BookingId} for event {EventId}. " +
                    "Seats: {AvailableBefore} -> {AvailableAfter}",
                    @event.BookingId,
                    @event.EventId,
                    availableBefore,
                    availableSeats);

            }
            catch (Exception ex)
            {
                // При ошибке сохранить результат и пробросить исключение 
                _logger.LogError(ex, "Error processing booking request {BookingId} for event {EventId}", @event.BookingId, @event.EventId);

                success = false;
                failureReason = $"Internal error: {ex.Message}";
                availableSeats = 0;

                await _processedBookingRepository.UpdateResultAsync(
                    @event.BookingId,
                    success,
                    failureReason,
                    availableSeats);

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing booking request {BookingId} for event {EventId}", @event.BookingId,  @event.EventId);

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

            return;
        }

        // Отправить результат
        await SendProcessedEvent(@event, success, failureReason, availableSeats);
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
            @event.BookingId.ToString(),
            processedEvent);

        _logger.LogInformation(
            "Sent BookingProcessed event for booking {BookingId}, Success: {Success}, Reason: {Reason}",
            @event.BookingId,
            success,
            failureReason ?? "none");
    }
}