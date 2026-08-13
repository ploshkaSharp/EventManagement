namespace EventManagement.Events.Application.Constants;

public static class CacheKeys
{
    public static string EventKey(Guid id) => $"event:{id}";
    public const string Top10Events = "events:top10";
}

// DTO для настроек TTL
public class CacheSettings
{
    public int Event { get; set; } = 600;
    public int Top10 { get; set; } = 300;
}