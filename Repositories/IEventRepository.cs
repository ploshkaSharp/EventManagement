using EventManagement.DTOs;
using EventManagement.Models;

namespace EventManagement.Repositories;

/// <summary>
/// Мероприятия (интерфейс репозитория)
/// </summary>
public interface IEventRepository
{
  /// <summary>
  /// Получить мероприятия по уникальному идентификатору
  /// </summary>
  /// <param name="id">ИД мероприятия</param>
  /// <returns></returns>
  Task<Event?> GetByIdAsync(Guid id);

  /// <summary>
  /// Получить список всех мероприятий
  /// </summary>
  /// <param name="filter">Параметры фильтра</param>
  /// <returns></returns>
  Task<IEnumerable<Event>> GetAllAsync(EventFilterDto? filter = null);

  /// <summary>
  /// Получить пагинированный список мероприятий
  /// </summary>
  /// <param name="filter">Параметры фильтра</param>
  /// <returns></returns>
  Task<PaginatedResult<Event>> GetPaginatedAsync(EventFilterDto filter);

  /// <summary>
  /// Создать мероприятие
  /// </summary>
  /// <param name="eventItem">Мероприятие</param>
  /// <returns></returns>    
  Task<Event> CreateAsync(Event eventItem);

  /// <summary>
  /// Обновить информацию о мероприятии
  /// </summary>
  /// <param name="eventItem">Мероприятие</param>
  /// <returns></returns>
  Task<Event?> UpdateAsync(Event eventItem);

  /// <summary>
  /// Удалить мероприятие
  /// </summary>
  /// <param name="id">ИД мероприятия</param>
  /// <returns></returns>
  Task<bool> DeleteAsync(Guid id);

  /// <summary>
  /// Забронировать место
  /// </summary>
  /// <param name="eventId">ИД мероприятия</param>
  /// <param name="count">Количество мест</param>
  /// <returns></returns>
  Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1);

/// <summary>
/// Освободить места
/// </summary>
/// <param name="eventId">ИД мероприятия</param>
/// <param name="count">Количество мест</param>
  /// <returns></returns>
  Task<bool> ReleaseSeatsAsync(Guid eventId, int count = 1);
}