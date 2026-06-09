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
  /// 
  /// </summary>
  /// <param name="eventItem"></param>
  /// <returns></returns>    
  Task<Event> CreateAsync(Event eventItem);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="eventItem"></param>
  /// <returns></returns>
  Task<Event?> UpdateAsync(Event eventItem);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  Task<bool> DeleteAsync(Guid id);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="eventId"></param>
  /// <param name="count"></param>
  /// <returns></returns>
  Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="eventId"></param>
  /// <param name="count"></param>
  /// <returns></returns>
  Task<bool> ReleaseSeatsAsync(Guid eventId, int count = 1);
}