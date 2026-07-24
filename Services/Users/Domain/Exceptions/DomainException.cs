namespace EventManagement.Domain.Exceptions;
/// <summary>
/// Исключения слоя Domain
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// 
    /// </summary>
    protected DomainException() : base() { }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    protected DomainException(string message) : base(message) { }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}