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

            var topics = new List<TopicSpecification>
            {
                new TopicSpecification
                {
                    Name = KafkaTopics.BookingRequested,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                },
                new TopicSpecification
                {
                    Name = KafkaTopics.BookingProcessed,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                },
                new TopicSpecification
                {
                    Name = KafkaTopics.BookingCancelled,  
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }                
            };

            foreach (var topic in topics)
            {
                try
                {
                    await adminClient.CreateTopicsAsync(new[] { topic });
                    _logger.LogInformation("Topic {Topic} created successfully", topic.Name);
                }
                catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    _logger.LogInformation("Topic {Topic} already exists, skipping creation", topic.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create topic {Topic}", topic.Name);
                }
            }

            // Получить список существующих топиков
            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var existingTopics = metadata.Topics.Select(t => t.Topic).ToList();
                _logger.LogInformation("Existing topics: {Topics}", string.Join(", ", existingTopics));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get topics metadata");
            }

            _logger.LogInformation("Kafka topic initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Kafka topics");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}