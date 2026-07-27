using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventManagement.Shared.Topics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;


namespace EventManagement.Events.Infrastructure.Kafka;

public class TopicInitializer : IHostedService
{
    private readonly ILogger<TopicInitializer> _logger;
    private readonly string _bootstrapServers;

    public TopicInitializer(IConfiguration configuration, ILogger<TopicInitializer> logger)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] 
            ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers
            }).Build();

            var topicSpecification = new TopicSpecification
            {
                Name = KafkaTopics.BookingConfirmed,
                NumPartitions = 3,
                ReplicationFactor = 1
            };

            await adminClient.CreateTopicsAsync(new[] { topicSpecification });
            _logger.LogInformation("Topic {Topic} created successfully", KafkaTopics.BookingConfirmed);
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Topic {Topic} already exists", KafkaTopics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create topic {Topic}", KafkaTopics.BookingConfirmed);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}