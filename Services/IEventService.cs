using EventManagement.DTOs;

namespace EventManagement.Services;

public interface IEventService
{
  IEnumerable<EventDTO> GetAll();
  EventDTO? GetById(Guid id);
  EventDTO Create(CreateEventDTO eventCreated);
  EventDTO? Update(Guid id, UpdateEventDTO eventUpdated);
  bool Delete(Guid id);
}