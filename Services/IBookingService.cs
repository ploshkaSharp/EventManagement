using EventManagement.DTOs;

namespace EventManagement.Services;

/// <summary>
/// Интерфейс сервиса управления бронированиями
/// </summary>
public interface IBookingService
{
  /// <summary>
  /// Создание брони указанного события
  /// </summary>
  /// <param name="eventId">Идентификатор события</param>
  /// <returns>Информация о созданной брони</returns>
  Task<BookingDTO> CreateBookingAsync(Guid eventId);

  /// <summary>
  /// Получение брони по идентификатору
  /// </summary>
  /// <param name="bookingId">Идентификатор брони</param>
  /// <returns>Информация о брони</returns>
  Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId);
}