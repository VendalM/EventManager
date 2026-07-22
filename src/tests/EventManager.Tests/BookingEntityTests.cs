using Bookings.Domain.Models;
using Contracts.Bookings;

namespace EventManager.Tests;

/// <summary>
/// Unit-тесты доменной сущности брони: проверяют локальные переходы статусов без БД и Kafka.
/// </summary>
public class BookingEntityTests
{
    /// <summary>
    /// Проверяет доменный переход брони в Confirmed.
    /// </summary>
    [Fact]
    public void Confirm_SetsConfirmedStatusAndProcessedAt()
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending
        };

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    /// <summary>
    /// Проверяет доменный переход брони в Rejected.
    /// </summary>
    [Fact]
    public void Reject_SetsRejectedStatusAndProcessedAt()
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending
        };

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    /// <summary>
    /// Проверяет доменный переход подтвержденной брони в Cancelled.
    /// </summary>
    [Fact]
    public void Cancel_SetsCancelledStatusAndProcessedAt()
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Confirmed
        };

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    /// <summary>
    /// Проверяет, что повторная отмена не перезаписывает время обработки.
    /// </summary>
    [Fact]
    public void Cancel_WhenAlreadyCancelled_DoesNotChangeProcessedAt()
    {
        var processedAt = DateTime.UtcNow.AddMinutes(-5);
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Cancelled,
            ProcessedAt = processedAt
        };

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);
    }
}
