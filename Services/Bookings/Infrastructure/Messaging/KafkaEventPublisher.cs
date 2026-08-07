using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Bookings.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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
    public async Task PublishAsync<T>(string topic, string key, T message)
    {
        try
        {
            var messageJson = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = messageJson
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage);

            _logger.LogInformation("Published message to topic {Topic}, key {Key}, offset {Offset}", topic, key, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Failed to publish message to topic {Topic}, key {Key}", topic, key);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}