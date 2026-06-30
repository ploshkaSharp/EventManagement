using EventManagement.Application.DTOs;
using EventManagement.Domain.Enums;

namespace EventManagement.Application.Services;

/// <summary>
/// Интерфейс сервиса управления бронированиями
/// </summary>
public interface IBookingService
{
  /// <summary>
  /// Создать бронь указанного события
  /// </summary>
  /// <param name="eventId">Идентификатор события</param>
  /// <returns>Информация о созданной брони</returns>
  Task<BookingDTO> CreateBookingAsync(Guid eventId);

  /// <summary>
  /// Получить бронь по идентификатору
  /// </summary>
  /// <param name="bookingId">Идентификатор брони</param>
  /// <returns>Информация о брони</returns>
  Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId);

  /// <summary>
  /// Получить список бронирований по статусу
  /// </summary>
  /// <param name="status">Статус бронирования</param>
  /// <returns>Список инфо о брони</returns>
  Task<IEnumerable<BookingDTO>> GetBookingByStatusAsync(BookingStatus status);

  /// <summary>
  /// Обновить статус брони
  /// </summary>
  /// <param name="bookingId">ИД брони</param>
  /// <param name="status">Новый статус</param>
  /// <returns>true если удалось обновить, fasle если не удалось</returns>
  Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);   
}