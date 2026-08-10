namespace EventManagement.Shared.Topics;

public static class KafkaTopics
{
    public const string BookingRequested = "booking-requested";
    public const string BookingProcessed = "booking-processed";
    public const string BookingCancelled = "booking-cancelled";
}