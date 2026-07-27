namespace EventManagement.Domain.Exceptions;

public class UnAuthorizedOperationException : DomainException
{
    public UnAuthorizedOperationException(string operation) 
        : base($"User is not authorized to perform '{operation}'") { }
    
    public UnAuthorizedOperationException(string operation, string reason) 
        : base($"User is not authorized to perform '{operation}'. Reason: {reason}") { }
}