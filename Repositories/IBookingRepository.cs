using EventManagement.Models;

namespace EventManagement.Repositories;

/// <summary>
/// Брони (интерфейс репозитория)
/// </summary>
public interface IBookingRepository
{
  /// <summary>
  /// получить бронь по ИД
  /// </summary>
  /// <param name="id">ИД брони</param>
  /// <returns></returns>
  Task<Booking?> GetByIdAsync(Guid id);

  /// <summary>
  /// Получить бронь по статусу
  /// </summary>
  /// <param name="status">Статус бронирования</param>
  /// <returns></returns>
  Task<IEnumerable<Booking>> GetBookingByStatusAsync(BookingStatus status);

  /// <summary>
  /// Создать бронь
  /// </summary>
  /// <param name="booking">Бронь</param>
  /// <returns></returns>
  Task<Booking> CreateAsync(Booking booking);

  /// <summary>
  /// Обновить бронь
  /// </summary>
  /// <param name="booking">Бронь</param>
  /// <returns></returns>
  Task<Booking?> UpdateAsync(Booking booking);
}