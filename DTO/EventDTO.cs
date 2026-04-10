using System.ComponentModel.DataAnnotations;
namespace EventManagement.DTOs;

/// <summary>
/// DTO мероприятия
/// </summary>
public class EventDTO
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
  /// Описание мероприятия (опционально)
  /// </summary>
  /// <example>Description of the technology conference</example>  
  public string? Description { get; set; }
  /// <summary>
  /// Дата и время начала мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example>  
  public DateTime StartAt { get; set; }
  /// <summary>
  /// Дата и время окончания мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example>  
  public DateTime EndAt { get; set; }
}

/// <summary>
/// DTO для создания нового мероприятия
/// </summary>
public class CreateEventDTO 
{
  /// <summary>
  /// Название мероприятия
  /// </summary>
  /// <example>Tech Conference 2026</example>
  [Required(ErrorMessage = "Title обязателен для заполнения.")]
  public string Title { get; set; } = string.Empty;
  /// <summary>
  /// Описание мероприятия (опционально)
  /// </summary>
  /// <example>Description of the technology conference</example>
  public string? Description { get; set; }
  /// <summary>
  /// Дата и время начала мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example> 
  [Required(ErrorMessage = "StartAt обязателен для заполнения.")] 
  public DateTime StartAt { get; set; }
  /// <summary>
  /// Дата и время окончания мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example>    
  [Required(ErrorMessage = "EndAt обязателен для заполнения.")]
  public DateTime EndAt { get; set; }
}

/// <summary>
/// DTO для создания обновления существующего мероприятия
/// </summary>
public class UpdateEventDTO
{
  /// <summary>
  /// Название мероприятия
  /// </summary>
  /// <example>Tech Conference 2026</example>  
  [Required(ErrorMessage = "Title обязателен для заполнения.")]
  public string Title { get; set; } = string.Empty;
  /// <summary>
  /// Описание мероприятия (опционально)
  /// </summary>
  /// <example>Description of the technology conference</example>
  public string? Description { get; set; }
  /// <summary>
  /// Дата и время начала мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example>   
  [Required(ErrorMessage = "StartAt обязателен для заполнения.")]
  public DateTime StartAt { get; set; }
  /// <summary>
  /// Дата и время окончания мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example>  
  [Required(ErrorMessage = "EndAt обязателен для заполнения.")]
  public DateTime EndAt { get; set; } 
}