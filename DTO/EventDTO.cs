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
  public DateTime StartAt { get; set; }
  [Required(ErrorMessage = "EndAt обязателен для заполнения.")]
  public DateTime EndAt { get; set; }
}