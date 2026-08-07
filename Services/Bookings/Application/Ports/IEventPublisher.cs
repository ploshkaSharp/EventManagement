using EventManagement.Shared.Contracts;

namespace EventManagement.Bookings.Application.Ports;

public interface IEventPublisher
{    
    Task PublishAsync<T>(string topic, string key, T message);
}