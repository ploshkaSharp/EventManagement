using EventManagement.Models;

namespace EventManagement.Services;

public class EventService : IEventService
{
  private readonly Dictionary<Guid, Event> _events = new();
  public IEnumerable<Event> GetAll()
  {
    return _events.Values.OrderBy(e => e.StartAt);
  }    
  public Event? GetById(Guid id)
  {
    _events.TryGetValue(id, out var eventItem);
    return eventItem;
  }
    
  public Event Create(Event eventCreated)
  {
    if (eventCreated.Id == Guid.Empty)
    {
      eventCreated.Id = Guid.NewGuid();
    }
                
    _events.TryAdd(eventCreated.Id, eventCreated);
    return eventCreated;
  }
    
  public Event? Update(Guid id, Event eventUpdated)
  {
    if (!_events.ContainsKey(id))
    {
      return null;
    }
          
    eventUpdated.Id = id;
    _events[id] = eventUpdated;

    return eventUpdated;
  }
    
  public bool Delete(Guid id)
  {
    return _events.Remove(id);
  }

}