namespace EventManagement.Domain.Exceptions;

/// <summary>
/// Исключение - Нет доступных мест 
/// </summary>
public class NoAvailableSeatsException : DomainException
{
  /// <summary>
  /// Список ошибок
  /// </summary>
  public IDictionary<string, string[]> Errors { get; }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  public NoAvailableSeatsException() : base("No available seats for this event")
  {
    Errors = new Dictionary<string, string[]>();
  }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  public NoAvailableSeatsException(string message) : base(message)
  {
    Errors = new Dictionary<string, string[]>();
  }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  /// <param name="errors">Список ошибок</param>
  public NoAvailableSeatsException(IDictionary<string, string[]> errors) 
        : base("No available seats for this event")
  {
    Errors = errors;
  }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  /// <param name="errors">Список ошибок</param>
  public NoAvailableSeatsException(string message, IDictionary<string, string[]> errors) 
        : base(message)
  {
    Errors = errors;
  }
}