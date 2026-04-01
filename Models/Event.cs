namespace EventManagement.Models;

/// <summary>
/// Мероприятие
/// </summary>
public class Event
{
  /// <summary>
  /// Уникальный идентификатор мероприятия
  /// </summary>
  /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>  
  public Guid Id { get; set; }
  /// <summary>
  /// Название мероприятия
  /// </summary>
  /// <example>Tech Conference 2026</example>
  public string Title { get; set; } = string.Empty;
  /// <summary>
  /// Описание мероприятия
  /// </summary>
  /// <example>Annual technology conference with industry experts</example>
  public string? Description { get; set; }
  /// <summary>
  /// Дата и время начала мероприятия (UTC)
  /// </summary>
  /// <example>2026-05-15T10:00:00Z</example>
  public DateTime StartAt { get; set; }
  /// <summary>
  /// Дата и время окончания мероприятия (UTC)
  /// </summary>
  /// <example>2026-05-15T18:00:00Z</example>
  public DateTime EndAt { get; set; }
}
