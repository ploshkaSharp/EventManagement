using EventManagement.Models;
using EventManagement.DTOs;

namespace EventManagement.Mappers;

public static class EventMapper
{   
  public static EventDTO ToDto(Event eventItem)
  {
    return new EventDTO
    {
      Id = eventItem.Id,
      Title = eventItem.Title,
      Description = eventItem.Description,
      StartAt = eventItem.StartAt,
      EndAt = eventItem.EndAt
    };
  }
    
  public static IEnumerable<EventDTO> ToDtoList(IEnumerable<Event> events)
  {
    return events.Select(ToDto);
  }

  public static Event ToEntity(CreateEventDTO createDto)
  {
    return new Event
    {
      Id = Guid.NewGuid(),
      Title = createDto.Title,
      Description = createDto.Description,
      StartAt = createDto.StartAt,
      EndAt = createDto.EndAt
    };
  }
    
  public static Event ToEntity(UpdateEventDTO updateDto, Guid id)
  {
    return new Event
    {
      Id = id,
      Title = updateDto.Title,
      Description = updateDto.Description,
      StartAt = updateDto.StartAt,
      EndAt = updateDto.EndAt
    };
  }  
}