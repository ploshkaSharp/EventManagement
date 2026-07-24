using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Shared.Contracts;
using EventManagement.Shared.Topics;
using EventManagement.Bookings.Application.Ports;

namespace EventManagement.Bookings.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IConfiguration configuration, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger.LogInformation("KafkaEventPublisher initialized");
    }

    public async Task PublishBookingConfirmedAsync(BookingConfirmedEvent @event)
    {
        try
        {
            var messageJson = JsonSerializer.Serialize(@event);
            
            var message = new Message<string, string>
            {
                Key = @event.EventId.ToString(),
                Value = messageJson
            };

            var result = await _producer.ProduceAsync(KafkaTopics.BookingConfirmed, message);
            
            _logger.LogInformation("Published BookingConfirmed event for booking {BookingId} to topic {Topic}, offset: {Offset}",
                @event.BookingId, KafkaTopics.BookingConfirmed, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish BookingConfirmed event for booking {BookingId}", @event.BookingId);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}