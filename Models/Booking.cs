namespace EventManagement.Models;

/// <summary>
/// Модель бронирования мероприятия
/// </summary>
public class Booking
{
  // Приватный конструктор для EF Core
  private Booking()
  {
    Event = null!;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="eventId"></param>
  public Booking(Guid eventId)
  {
    Id = Guid.NewGuid();
    EventId = eventId;
    Status = BookingStatus.Pending;
    CreatedAt = DateTime.UtcNow;
    Event = null!;
  }  

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
  /// Дата и время создания брони
  /// </summary>
  /// <example>2026-05-15T10:00:00Z</example>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Дата и время обработки брони
  /// </summary>
  /// <example>2026-05-15T11:00:00Z</example>
  public DateTime? ProcessedAt { get; set; }

  /// <summary>
  /// Навигационное свойство 
  /// </summary>
  public Event Event { get; private set; }

  /// <summary>
  /// Подтвердить бронирование
  /// </summary>
  /// <exception cref="InvalidOperationException"></exception>
  public void Confirm()
  {
    if (Status != BookingStatus.Pending)
      throw new InvalidOperationException($"Cannot confirm booking with status {Status}");

    Status = BookingStatus.Confirmed;
    ProcessedAt = DateTime.UtcNow;
  }

  /// <summary>
  /// Отменить бронь
  /// </summary>
  /// <exception cref="InvalidOperationException"></exception>
  public void Reject()
  {
    if (Status != BookingStatus.Pending)
      throw new InvalidOperationException($"Cannot reject booking with status {Status}");

    Status = BookingStatus.Rejected;
    ProcessedAt = DateTime.UtcNow;
  }
}