using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Events.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventManagement.Events.Infrastructure.Messaging;

/// <summary>
/// Реализация публикатора событий через Kafka
/// </summary>
public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private bool _disposed;

    public KafkaEventPublisher(
        IConfiguration configuration,
        ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100,
            RequestTimeoutMs = 5000
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Error}", e.Reason))
            .Build();
        
        _logger.LogInformation("KafkaEventPublisher initialized with BootstrapServers: {BootstrapServers}", 
            bootstrapServers);
    }

    public async Task PublishAsync<T>(string topic, string key, T message)
    {
        try
        {
            var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = messageJson
            };

            _logger.LogDebug(
                "Publishing message to topic {Topic}, key {Key}, type {Type}",
                topic,
                key,
                typeof(T).Name);

            var result = await _producer.ProduceAsync(topic, kafkaMessage);
            
            _logger.LogInformation(
                "Successfully published message to topic {Topic}, key {Key}, partition {Partition}, offset {Offset}",
                topic,
                key,
                result.Partition,
                result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Kafka produce error for topic {Topic}, key {Key}: {Error}",
                topic,
                key,
                ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing message to topic {Topic}, key {Key}",
                topic,
                key);
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _producer?.Dispose();
            _logger.LogInformation("KafkaEventPublisher disposed");
        }
        
        _disposed = true;
    }
}