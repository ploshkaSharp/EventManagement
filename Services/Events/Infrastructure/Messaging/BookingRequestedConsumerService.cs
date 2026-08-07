using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Shared.Contracts;
using EventManagement.Shared.Topics;
using EventManagement.Events.Application.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventManagement.Events.Infrastructure.Messaging;

public class BookingRequestedConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingRequestedConsumerService> _logger;
    private readonly IConsumer<string, string> _consumer;

    public BookingRequestedConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingRequestedConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"] 
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");
        var groupId = configuration["Kafka:ConsumerGroup"];
        
        // Если ConsumerGroup не задан, используем значение по умолчанию
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

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingRequestedConsumerService started");
        _consumer.Subscribe(KafkaTopics.BookingRequested);

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

                    var @event = JsonSerializer.Deserialize<BookingRequestedEvent>(
                        consumeResult.Message.Value);

                    if (@event == null)
                    {
                        _logger.LogWarning("Failed to deserialize message");
                        _consumer.Commit(consumeResult);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IBookingRequestedHandler>();

                    await handler.HandleAsync(@event);

                    _consumer.Commit(consumeResult);
                    _logger.LogInformation("Successfully processed booking request {BookingId} at offset {Offset}", @event.BookingId, consumeResult.Offset);
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

        _logger.LogInformation("BookingRequestedConsumerService stopped");
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}