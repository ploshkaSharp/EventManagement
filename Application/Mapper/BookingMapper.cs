using EventManagement.Domain.Entities;
using EventManagement.Application.DTOs;

namespace EventManagement.Application.Mappers;

/// <summary>
/// Маппер для соспоставления
/// </summary>
public static class BookingMapper
{
  /// <summary>
  /// Маппинг модели Booking в BookingDTO
  /// </summary>
  /// <param name="booking">Модель бронирования</param>
  /// <returns>DTO объект мероприятия</returns>
  public static BookingDTO ToDto(Booking booking)
  {
    return new BookingDTO
    {
      Id = booking.Id,
      EventId = booking.EventId,
      UserId = booking.UserId,
      Status = booking.Status,
      CreatedAt = booking.CreatedAt,
      ProcessedAt = booking.ProcessedAt
    };
  }
}