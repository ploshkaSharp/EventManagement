namespace EventManagement.Models;

/// <summary>
/// Модель бронирования мероприятия
/// </summary>
public class Booking
{
  /// <summary>
  /// Уникальный идентификатор брони
  /// </summary>
  /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
  public Guid Id { get; set; }

  /// <summary>
  /// Идентификатор мероприятия, к которому относится бронь
  /// </summary>
  /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
  public Guid EventId { get; set; }

  /// <summary>
  /// Текущий статус брони
  /// </summary>
  /// <example>Confirmed</example>
  public BookingStatus Status { get; set; }

  /// <summary>
  /// Дата и время создания брони (UTC)
  /// </summary>
  /// <example>2026-05-15T10:00:00Z</example>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Дата и время обработки брони (UTC)
  /// </summary>
  /// <example>2026-05-15T11:00:00Z</example>
  public DateTime? ProcessedAt { get; set; }
}