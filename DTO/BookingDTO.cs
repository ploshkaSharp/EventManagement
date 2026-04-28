using EventManagement.Models;

namespace EventManagement.DTOs;

/// <summary>
/// DTO для информации о бронировании
/// </summary>
public class BookingDTO
{
  /// <summary>
  /// Уникальный идентификатор брони
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Идентификатор мероприятия
  /// </summary>
  public Guid EventId { get; set; }

  /// <summary>
  /// Статус бронирования
  /// </summary>
  public BookingStatus Status { get; set; }

  /// <summary>
  /// Дата и время создания брони
  /// </summary>
  public DateTimeOffset CreatedAt { get; set; }

  /// <summary>
  /// Дата и время обработки брони
  /// </summary>
  public DateTimeOffset? ProcessedAt { get; set; }
}