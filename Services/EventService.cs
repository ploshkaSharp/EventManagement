using EventManagement.Models;
using EventManagement.DTOs;
using EventManagement.Mappers;

namespace EventManagement.Services;

/// <summary>
/// Мероприятие
/// </summary>
public class EventService : IEventService
{
  private readonly Dictionary<Guid, Event> _events = new();
  /// <summary>
  /// Получить список всех мероприятий
  /// </summary>
  /// <returns>Список мероприятий</returns>
  public IEnumerable<EventDTO> GetAll()
  {
    var events = _events.Values.OrderBy(e => e.StartAt);
    return EventMapper.ToDtoList(events);
  }    

  /// <summary>
  /// Получить мероприятие по идентификатору
  /// </summary>
  /// <param name="id">Идентификатор мероприятия (GUID)</param>
  /// <returns>DTO мероприятия с указанным идентификатором если оно найдено или null</returns>  
  public EventDTO? GetById(Guid id)
  {
    _events.TryGetValue(id, out var eventItem);
    return eventItem != null ? EventMapper.ToDto(eventItem) : null;    
  }

  /// <summary>
  /// Создать новое мероприятие
  /// </summary>
  /// <param name="eventCreated">Данные для создания мероприятия</param>
  /// <returns>DTO созданного мероприятия</returns>    
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
    
  /// <summary>
  /// Обновить существующее мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для обновления (GUID)</param>
  /// <param name="eventUpdated">Обновленные данные мероприятия</param>
  /// <returns>DTO обновленного мероприятия если оно найдено или null</returns>    
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
    
  /// <summary>
  /// Удалить мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для удаления (GUID)</param>
  /// <returns>true если удалось удалить или false</returns>
  public bool Delete(Guid id)
  {
    return _events.Remove(id);    
  }
}