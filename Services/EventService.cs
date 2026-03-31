using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Mappers;

namespace EventManagement.Services;

public class EventService : IEventService
{
  private readonly Dictionary<Guid, Event> _events = new();
  public IEnumerable<EventDTO> GetAll()
  {
    var events = _events.Values.OrderBy(e => e.StartAt);
    return EventMapper.ToDtoList(events);
  }    
  public EventDTO? GetById(Guid id)
  {
    _events.TryGetValue(id, out var eventItem);
    return eventItem != null ? EventMapper.ToDto(eventItem) : null;    
  }
    
  public EventDTO Create(CreateEventDTO eventCreated)
  {
    var eventItem = EventMapper.ToEntity(eventCreated);
    
    if (eventItem.Id == Guid.Empty)
    {
      eventItem.Id = Guid.NewGuid();
    }
                
    _events.TryAdd(eventItem.Id, eventItem);
    return EventMapper.ToDto(eventItem);
  }
    
  public EventDTO? Update(Guid id, UpdateEventDTO eventUpdated)
  {
    if (!_events.ContainsKey(id))
    {
      return null;
    }

    var updatedEvent = EventMapper.ToEntity(eventUpdated, id);          
    _events[id] = updatedEvent;

    return EventMapper.ToDto(updatedEvent);
  }
    
  public bool Delete(Guid id)
  {
    return _events.Remove(id);    
  }
}