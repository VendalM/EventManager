using Contracts.Messages;

namespace Bookings.Application.Interfaces;

public interface IBookingStatusHandler
{
    Task HandleBookingConfirmedAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default);

    Task HandleBookingRejectedAsync(
        BookingRejected message,
        CancellationToken cancellationToken = default);
}