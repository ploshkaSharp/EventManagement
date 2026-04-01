using EventManagement.Models;
using EventManagement.DTOs;

namespace EventManagement.Mappers;

/// <summary>
/// Маппер для соспоставления
/// </summary>
public static class EventMapper
{   
  /// <summary>
  /// Маппинг в DTO мероприятия
  /// </summary>
  /// <param name="eventItem">Мероприятие</param>
  /// <returns>DTO объект мероприятия</returns>
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

  /// <summary>
  /// Маппинг в список DTO мероприятия
  /// </summary>
  /// <param name="events">Список мероприятий</param>
  /// <returns>Список с DTO объектами мероприятий</returns>  
  public static IEnumerable<EventDTO> ToDtoList(IEnumerable<Event> events)
  {
    return events.Select(ToDto);
  }

  /// <summary>
  /// Маппинг в DTO при создании
  /// </summary>
  /// <param name="createDto">Новое мероприятие</param>
  /// <returns>DTO объект мероприятия</returns>
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

  /// <summary>
  /// Маппинг в DTO при обнолвении
  /// </summary>
  /// <param name="updateDto">Обновляемое мероприятие</param>
  /// <param name="id">Идентификатор мероприятие</param>
  /// <returns>DTO объект мероприятия</returns>    
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