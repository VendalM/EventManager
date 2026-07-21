namespace Contracts.Messages;

/// <summary>
/// Сообщения между сервисами
/// </summary>
public static class BookingTopics
{
    public const string BookingRequested = "booking-requested";
    public const string BookingConfirmed = "booking-confirmed";
    public const string BookingRejected = "booking-rejected";
    public const string BookingCancelled = "booking-cancelled";
}