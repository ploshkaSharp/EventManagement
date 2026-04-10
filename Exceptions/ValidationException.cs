using System.Collections.Generic;

namespace EventManagement.Exceptions;

/// <summary>
/// Исключение - Ошибка валидации
/// </summary>
public class ValidationException : Exception
{
  public IDictionary<string, string[]> Errors { get; }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  public ValidationException() : base("Validation error occurred")
  {
    Errors = new Dictionary<string, string[]>();
  }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  public ValidationException(string message) : base(message)
  {
    Errors = new Dictionary<string, string[]>();
  }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  /// <param name="errors">Список ошибок</param>
  public ValidationException(IDictionary<string, string[]> errors) 
        : base("Validation error occurred")
  {
    Errors = errors;
  }
  /// <summary>
  /// Ошибка валидации
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  /// <param name="errors">Список ошибок</param>
  public ValidationException(string message, IDictionary<string, string[]> errors) 
        : base(message)
  {
    Errors = errors;
  }
}