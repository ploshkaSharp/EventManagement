using System.Text.Json;
using StackExchange.Redis;
using EventManagement.Events.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventManagement.Events.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>((string)value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis Get error for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _database.StringSetAsync(key, json, (Expiration)expiry!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis Set error for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis Remove error for key {Key}", key);
        }
    }
}
