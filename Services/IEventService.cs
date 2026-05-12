using EventManagement.DTOs;

namespace EventManagement.Services;

/// <summary>
/// Мероприятия (интерфейс)
/// </summary>
public interface IEventService
{
  /// <summary>
  /// Получить список всех мероприятий
  /// </summary>  
  /// <param name="filter">Параметры фильтра</param>
  IEnumerable<EventDTO> GetAll(EventFilterDto? filter = null);
  /// <summary>
  /// Получить пагинированный список мероприятий
  /// </summary>
  /// <param name="filter">Параметры фильтра</param>
  PaginatedResult<EventDTO> GetPaginated(EventFilterDto filter);
  /// <summary>
  /// Получить мероприятия по уникальному идентификатору
  /// </summary>
  /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
  EventDTO? GetById(Guid id);
  /// <summary>
  /// Создать новое мероприятие
  /// </summary>
  /// <param name="eventCreated">Данные для создания мероприятия</param>
  EventDTO Create(CreateEventDTO eventCreated);
  /// <summary>
  /// Обновить существующее мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для обновления (GUID)</param>
  /// <param name="eventUpdated">Обновленные данные мероприятия</param>  
  EventDTO? Update(Guid id, UpdateEventDTO eventUpdated);
  /// <summary>
  /// Удалить мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для удаления (GUID)</param>  
  bool Delete(Guid id);
  /// <summary>
  /// Попытка забронировать места на мероприятии
  /// </summary>
  /// <result>true - бронирование удалось, false - не удалось</result>
  bool TryReserveSeats(Guid eventId, int count = 1);
  /// <summary>
  /// Вернуть забронированые места
  /// </summary>
  /// <result>true - возврат удался, false - не удалось</result>
  bool ReleaseSeats(Guid eventId, int count = 1);  
}