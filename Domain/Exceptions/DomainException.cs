namespace EventManagement.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException() : base() { }
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}