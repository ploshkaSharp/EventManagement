using EventManagement.DTOs;
using EventManagement.Models;

namespace EventManagement.Services;

/// <summary>
/// Интерфейс сервиса управления бронированиями
/// </summary>
public interface IBookingService
{
  /// <summary>
  /// Создать бронь указанного события
  /// </summary>
  /// <param name="eventId">Идентификатор события</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns>Информация о созданной брони</returns>
  Task<BookingDTO> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken);

  /// <summary>
  /// Получить бронь по идентификатору
  /// </summary>
  /// <param name="bookingId">Идентификатор брони</param>
  /// <param name="cancellationToken">Токен отмены</param> 
  /// <returns>Информация о брони</returns>
  Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken);

  /// <summary>
  /// Получить список бронирований по статусу
  /// </summary>
  /// <param name="status">Статус бронирования</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns>Список инфо о брони</returns>
  Task<IEnumerable<BookingDTO>> GetBookingByStatusAsync(BookingStatus status, CancellationToken cancellationToken);

  /// <summary>
  /// Обновить статус брони
  /// </summary>
  /// <param name="bookingId">ИД брони</param>
  /// <param name="status">Новый статус</param>
  /// <param name="cancellationToken">Токен отмены</param>
  /// <returns>true если удалось обновить, fasle если не удалось</returns>
  Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken);   
}