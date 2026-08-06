using System.Text.Json;
using Confluent.Kafka;
using EventManagement.Shared.Contracts;
using EventManagement.Shared.Topics;
using EventManagement.Events.Application.Handlers;
using EventManagement.Events.Infrastructure.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;


namespace EventManagement.Events.Infrastructure.Messaging;

public class BookingConfirmedConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConfirmedConsumerService> _logger;
    private readonly IConsumer<string, string> _consumer;

    public BookingConfirmedConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingConfirmedConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"] 
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");
        var groupId = configuration["Kafka:ConsumerGroup"] 
            ?? throw new InvalidOperationException("Kafka:ConsumerGroup not configured");

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Отключить автокоммит - коммит только после успешной обработки
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

                    if (@event == null)
                    {
                        _logger.LogWarning("Failed to deserialize message: {Message}", consumeResult.Message.Value);
                        // Пропустить невалидное сообщение с коммитом
                        _consumer.Commit(consumeResult);
                        continue;
                    }

                    // Обрабатать сообщение идемпотентно
                    var processed = await ProcessMessageAsync(@event, stoppingToken);

                    if (processed)
                    {
                        // Коммит оффсета только после успешной обработки
                        _consumer.Commit(consumeResult);
                        _logger.LogInformation("Successfully processed and committed booking {BookingId} at offset {Offset}",
                                               @event.BookingId, consumeResult.Offset);
                    }
                    else
                    {
                        // Не коммитить - сообщение будет обработано повторно при следующем чтении
                        _logger.LogWarning("Message for booking {BookingId} not processed, will retry", @event.BookingId);
                        await Task.Delay(500, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error");
                    await Task.Delay(500, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await Task.Delay(500, stoppingToken);
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

    private async Task<bool> ProcessMessageAsync(BookingConfirmedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var handler = scope.ServiceProvider.GetRequiredService<IBookingConfirmedHandler>();
            
            // 1. Проверить, не было ли это бронирование уже обработано (идемпотентность)
            var alreadyProcessed = await dbContext.ProcessedBookings.AnyAsync(pb => pb.BookingId == @event.BookingId, cancellationToken);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Booking {BookingId} already processed, skipping duplicate", @event.BookingId);
                return true; // успешно, чтобы закоммитить и не обрабатывать повторно
            }

            // 2. Обрабатать бронирование (уменьшить места)
            await handler.HandleAsync(@event);

            // 3. Сохранить запись обработанной брони (идемпотентность)
            // INSERT ... ON CONFLICT DO NOTHING для защиты от дублей
            await dbContext.Database.ExecuteSqlRawAsync(
                @"
                    INSERT INTO ""ProcessedBookings"" (""BookingId"", ""ProcessedAt"", ""EventId"", ""UserId"")
                    VALUES ({0}, {1}, {2}, {3})
                    ON CONFLICT (""BookingId"") DO NOTHING
                ",
                @event.BookingId,
                DateTime.UtcNow,
                @event.EventId,
                @event.UserId,
                cancellationToken);

            _logger.LogInformation("Booking {BookingId} for event {EventId} processed successfully", @event.BookingId, @event.EventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing booking {BookingId} for event {EventId}", @event.BookingId, @event.EventId);
            return false;
        }
    }    

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}