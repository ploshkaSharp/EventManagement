namespace EventManagement.Domain.Exceptions;

public class EventAlreadyStartedException : DomainException
{
    public EventAlreadyStartedException(string eventTitle) 
        : base($"Event '{eventTitle}' has already started") { }
    
    public EventAlreadyStartedException(string message, Exception innerException) 
        : base(message, innerException) { }
}