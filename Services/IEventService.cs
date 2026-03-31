using EventManagement.Models;

namespace EventManagement.Services;

public interface IEventService
{
  IEnumerable<Event> GetAll();
  Event? GetById(Guid id);
  Event Create(Event eventCreated);
  Event? Update(Guid id, Event eventUpdated);
  bool Delete(Guid id);
}