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
  [StartDateValidation]
  public DateTime StartAt { get; set; }
  /// <summary>
  /// Дата и время окончания мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example>    
  [Required(ErrorMessage = "EndAt обязателен для заполнения.")]
  public DateTime EndAt { get; set; }

  /*
  /// <summary>
  /// Кастомная валидация для проверки корректности дат
  /// </summary>
  /// <param name="validationContext">Контекст валидации</param>
  /// <returns>Результаты валидации</returns>
  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  { 
    var results = new List<ValidationResult>();

    // Проверка, что дата начала не в прошлом
    if (StartAt < DateTime.UtcNow)
    {
      results.Add(new ValidationResult("StartAt cannot be in the past",  new[] { nameof(StartAt) }));
    }

    // Проверка, что дата начала меньше даты окончания
    if (StartAt >= EndAt)
    {
      results.Add(new ValidationResult("StartAt cannot be in the past", new[] { nameof(StartAt) }));      
    }
        
    return results;
  }
  */
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
  [StartDateValidation]
  public DateTime StartAt { get; set; }
  /// <summary>
  /// Дата и время окончания мероприятия (UTC)
  /// </summary>
  /// <example>2026-06-15T10:00:00Z</example>  
  [Required(ErrorMessage = "EndAt обязателен для заполнения.")]
  public DateTime EndAt { get; set; }
}

/// <summary>
/// Валидация даты начала мероприятия
/// </summary>
public class StartDateValidation : ValidationAttribute 
{
  /// <summary>
  /// Проверить, что дата начала меньше даты окончания
  /// </summary>
  /// <param name="value">Значение для проверки</param>
  /// <param name="validationContext">Контекст валидации</param>
  /// <returns>Результат валидации</returns>
  protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
  {
    var valueString = value != null ? value.ToString() : null;
    
    if (string.IsNullOrEmpty(valueString))
    {
      return new ValidationResult("StartAt обязателен для заполнения.");       
    }

    // TODO: Без рефлексии как-то можно обойтись?
    var dtoName = validationContext.ObjectType.Name; 
    DateTime.TryParse(valueString, out DateTime startAt);
    
    if(dtoName == "UpdateEventDTO")
    {
      var model = (UpdateEventDTO)validationContext.ObjectInstance; 
      if (DateTime.Compare(startAt, model.EndAt) > 0)
      {
        return new ValidationResult("Дата StartAt должна быть меньше чем EndAt.");
      }        
    }    
    else if (dtoName == "CreateEventDTO")
    {
      var model = (CreateEventDTO)validationContext.ObjectInstance;
      if (DateTime.Compare(startAt, model.EndAt) > 0)
      {    
        return new ValidationResult("Дата StartAt должна быть меньше чем EndAt.");
      }      
    }      
        
    return ValidationResult.Success;
  }
}

