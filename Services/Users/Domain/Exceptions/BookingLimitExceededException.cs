namespace EventManagement.Domain.Exceptions;

public class BookingLimitExceededException : DomainException
{
    public int Limit { get; }
    
    public BookingLimitExceededException(int limit) 
        : base($"Cannot create booking. User has reached the maximum limit of {limit} active bookings")
    {
        Limit = limit;
    }
    
    public BookingLimitExceededException(string message, int limit) 
        : base(message)
    {
        Limit = limit;
    }
}