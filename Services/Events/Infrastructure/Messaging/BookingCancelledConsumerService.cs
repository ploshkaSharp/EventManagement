using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Shared.Contracts;
using EventManagement.Shared.Topics;
using EventManagement.Events.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace EventManagement.Events.Infrastructure.Messaging;

/// <summary>
/// Consumer для обработки событий отмены бронирования
/// </summary>
public class BookingCancelledConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingCancelledConsumerService> _logger;
    private readonly IConsumer<string, string> _consumer;

    public BookingCancelledConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingCancelledConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");

        var groupId = configuration["Kafka:ConsumerGroup"];
        if (string.IsNullOrEmpty(groupId))
        {
            groupId = "bookings-service-group";
            _logger.LogWarning("Kafka:ConsumerGroup not configured, using default: {GroupId}", groupId);
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingCancelledConsumerService started");
        _consumer.Subscribe(KafkaTopics.BookingCancelled);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(5));

                    if (consumeResult?.Message == null)
                    {
                        await Task.Delay(100, stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Received BookingCancelled event from topic {Topic}, partition {Partition}, offset {Offset}",
                        consumeResult.Topic,
                        consumeResult.Partition,
                        consumeResult.Offset);

                    var @event = JsonSerializer.Deserialize<BookingCancelledEvent>(consumeResult.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (@event == null)
                    {
                        _logger.LogWarning("Failed to deserialize BookingCancelledEvent");
                        _consumer.Commit(consumeResult);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                    // Освободить место
                    await eventService.ReleaseSeatsAsync(@event.EventId, 1);

                    _logger.LogInformation("Successfully released seats for event {EventId} from cancelled booking {BookingId}", @event.EventId, @event.BookingId);

                    _consumer.Commit(consumeResult);
                    _logger.LogInformation("Successfully processed BookingCancelled event for booking {BookingId} at offset {Offset}", @event.BookingId, consumeResult.Offset);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in consumer loop");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
        }

        _logger.LogInformation("BookingCancelledConsumerService stopped");
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}