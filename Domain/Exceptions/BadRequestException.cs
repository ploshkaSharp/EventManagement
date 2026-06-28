namespace EventManagement.Domain.Exceptions;

/// <summary>
/// Исключение - Некорректный запрос
/// </summary>
public class BadRequestException : DomainException
{
  /// <summary>
  /// Некорректный запрос
  /// </summary>
  public BadRequestException() : base() { }
  /// <summary>
  /// Некорректный запрос
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  public BadRequestException(string message) : base(message) { }
  /// <summary>
  /// Некорректный запрос
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  /// <param name="innerException">Исключение</param>
  public BadRequestException(string message, Exception innerException) 
        : base(message, innerException) { }
}