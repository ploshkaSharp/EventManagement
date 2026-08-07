namespace EventManagement.Events.Application.Ports;

/// <summary>
/// Интерфейс для публикации событий в Kafka
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Публикация сообщения в указанный топик
    /// </summary>
    /// <typeparam name="T">Тип сообщения</typeparam>
    /// <param name="topic">Имя топика</param>
    /// <param name="key">Ключ сообщения (для партиционирования)</param>
    /// <param name="message">Сообщение</param>
    Task PublishAsync<T>(string topic, string key, T message);
}