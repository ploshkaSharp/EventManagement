using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Shared.Contracts;
using EventManagement.Shared.Topics;
using EventManagement.Events.Application.Handlers;

namespace EventManagement.Events.Infrastructure.Messaging;

public class BookingConfirmedConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConfirmedConsumerService> _logger;
    private readonly string _bootstrapServers;
    private readonly string _groupId;
    private readonly IConsumer<string, string> _consumer;

    public BookingConfirmedConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingConfirmedConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] 
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");
        _groupId = configuration["Kafka:ConsumerGroup"] 
            ?? throw new InvalidOperationException("Kafka:ConsumerGroup not configured");

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingConfirmedConsumerService started");
        _consumer.Subscribe(KafkaTopics.BookingConfirmed);

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

                    _logger.LogInformation("Received message: {Message}", consumeResult.Message.Value);

                    var @event = JsonSerializer.Deserialize<BookingConfirmedEvent>(
                        consumeResult.Message.Value);

                    if (@event != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IBookingConfirmedHandler>();
                        await handler.HandleAsync(@event);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            }
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
        }

        _logger.LogInformation("BookingConfirmedConsumerService stopped");
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}