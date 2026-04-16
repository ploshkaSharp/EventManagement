namespace EventManagement.Exceptions;

/// <summary>
/// Исключение - Ресурс не найден
/// </summary>
public class NotFoundException : Exception
{
  /// <summary>
  /// Ресурс не найден
  /// </summary>
  public NotFoundException() : base() { }
  /// <summary>
  /// Ресурс не найден
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  public NotFoundException(string message) : base(message) { }
  /// <summary>
  /// Ресурс не найден
  /// </summary>
  /// <param name="message">Текст ошибки</param>
  /// <param name="innerException">Исключение</param>
  public NotFoundException(string message, Exception innerException) 
        : base(message, innerException) { }
  /// <summary>
  /// Ресурс не найден
  /// </summary>
  /// <param name="entityName">Имя сущности</param>
  /// <param name="id">Идентификатор</param>
  public NotFoundException(string entityName, object id) 
        : base($"Entity '{entityName}' with id '{id}' was not found") { }
}