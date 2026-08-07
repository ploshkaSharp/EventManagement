using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Shared.Contracts;
using EventManagement.Shared.Topics;
using EventManagement.Bookings.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


namespace EventManagement.Bookings.Infrastructure.Messaging;

public class BookingProcessedConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessedConsumerService> _logger;
    private readonly IConsumer<string, string> _consumer;

    public BookingProcessedConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingProcessedConsumerService> logger)
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
            EnablePartitionEof = true,
            AllowAutoCreateTopics = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingProcessedConsumerService started");
        _consumer.Subscribe(KafkaTopics.BookingProcessed);

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

                    _logger.LogInformation("Received message from topic {Topic}, partition {Partition}, offset {Offset}",
                        consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

                    _logger.LogInformation("Message = {Key} - {Message}", consumeResult.Message.Key, consumeResult.Message.Value);

                    var @event = JsonSerializer.Deserialize<BookingProcessedEvent>(consumeResult.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (@event == null)
                    {
                        _logger.LogWarning("Failed to deserialize message: {Message}", consumeResult.Message.Value);
                        _consumer.Commit(consumeResult);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    await bookingService.ProcessBookingResultAsync(@event);

                    _consumer.Commit(consumeResult);
                    _logger.LogInformation(
                        "Successfully processed BookingProcessed event for booking {BookingId} at offset {Offset}",
                        @event.BookingId,
                        consumeResult.Offset);
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

        _logger.LogInformation("BookingProcessedConsumerService stopped");
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}