using EventManagement.Application.DTOs;

namespace EventManagement.Application.Services;

/// <summary>
/// Мероприятия (интерфейс)
/// </summary>
public interface IEventService
{
  /// <summary>
  /// Получить список всех мероприятий
  /// </summary>  
  /// <param name="filter">Параметры фильтра</param>
  Task<IEnumerable<EventDTO>> GetAllAsync(EventFilterDto? filter = null);
  /// <summary>
  /// Получить пагинированный список мероприятий
  /// </summary>
  /// <param name="filter">Параметры фильтра</param>
  Task<PaginatedResult<EventDTO>> GetPaginatedAsync(EventFilterDto filter);
  /// <summary>
  /// Получить мероприятия по уникальному идентификатору
  /// </summary>
  /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
  Task<EventDTO?> GetByIdAsync(Guid id);
  /// <summary>
  /// Создать новое мероприятие
  /// </summary>
  /// <param name="eventCreated">Данные для создания мероприятия</param>
  Task<EventDTO> CreateAsync(CreateEventDTO eventCreated);
  /// <summary>
  /// Обновить существующее мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для обновления (GUID)</param>
  /// <param name="eventUpdated">Обновленные данные мероприятия</param>  
  Task<EventDTO?> UpdateAsync(Guid id, UpdateEventDTO eventUpdated);
  /// <summary>
  /// Удалить мероприятие
  /// </summary>
  /// <param name="id">Идентификатор мероприятия для удаления (GUID)</param>  
  Task<bool> DeleteAsync(Guid id);
  /// <summary>
  /// Попытка забронировать места на мероприятии
  /// </summary>
  /// <result>true - бронирование удалось, false - не удалось</result>
  Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1);
  /// <summary>
  /// Вернуть забронированые места
  /// </summary>
  /// <result>true - возврат удался, false - не удалось</result>
  Task<bool> ReleaseSeatsAsync(Guid eventId, int count = 1);  
}