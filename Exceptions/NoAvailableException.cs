namespace EventManagement.Exceptions;

/// <summary>
/// Исключение - Нет доступных мест 
/// </summary>
public class NoAvailableSeatsException : Exception
{
    /// <summary>
    /// Нет доступных мест для этого мероприятия
    /// </summary>
    public NoAvailableSeatsException() : base("No available seats for this event") { }
    /// <summary>
    /// Нет доступных мест для этого мероприятия
    /// </summary>
    /// <param name="message">Текст ошибки</param>
    public NoAvailableSeatsException(string message) : base(message) { }
    /// <summary>
    /// Нет доступных мест для этого мероприятия
    /// </summary>
    /// <param name="message">Текст ошибки</param>
    /// <param name="innerException">Исключение</param>    
    public NoAvailableSeatsException(string message, Exception innerException)
        : base(message, innerException) { }
}