using System.ComponentModel.DataAnnotations;
namespace EventManagement.DTOs;


public class EventDTO
{
  public Guid Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public string? Description { get; set; }
  public DateTime StartAt { get; set; }
  public DateTime EndAt { get; set; }
}

public class CreateEventDTO
{
  [Required(ErrorMessage = "Title обязателен для заполнения.")]
  public string Title { get; set; } = string.Empty;
  public string? Description { get; set; }

  [Required(ErrorMessage = "StartAt обязателен для заполнения.")]
  [StartDateValidation]
  public DateTime StartAt { get; set; }
  [Required(ErrorMessage = "EndAt обязателен для заполнения.")]
  public DateTime EndAt { get; set; }
}

public class UpdateEventDTO
{
  [Required(ErrorMessage = "Title обязателен для заполнения.")]
  public string Title { get; set; } = string.Empty;
  public string? Description { get; set; }
  
  [Required(ErrorMessage = "StartAt обязателен для заполнения.")]
  [StartDateValidation]
  public DateTime StartAt { get; set; }
  [Required(ErrorMessage = "EndAt обязателен для заполнения.")]
  public DateTime EndAt { get; set; }
}

public class StartDateValidation : ValidationAttribute 
{
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

