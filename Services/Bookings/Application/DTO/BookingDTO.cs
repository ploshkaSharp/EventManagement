using EventManagement.Domain.Enums;

namespace EventManagement.Application.DTOs;

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
  /// Идентификатор пользователя
  /// </summary>
  public Guid UserId { get; set; }  

  /// <summary>
  /// Статус бронирования
  /// </summary>
  public BookingStatus Status { get; set; }

  /// <summary>
  /// Дата и время создания брони
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Дата и время обработки брони
  /// </summary>
  public DateTime? ProcessedAt { get; set; }
}