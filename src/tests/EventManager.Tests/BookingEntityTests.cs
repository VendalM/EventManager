using EventManager.Enums;
using EventManager.Models;

namespace EventManager.Tests;

/// <summary>
/// Набор тестов для сущности бронирования
/// </summary>
public class BookingEntityTests
{
    /// <summary>
    /// Проверка создания бронирования со статусом Pending
    /// </summary>
    [Fact]
    public void CreatePending_SetsCorrectStatus()
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending
        };
        
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }
    
    /// <summary>
    /// Проверка смены статуса на Confirmed
    /// </summary>
    [Fact]
    public void Confirm_ChangesStatusToConfirmed()
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending
        };
        
        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = DateTime.UtcNow;
        
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
    
    /// <summary>
    /// Проверка смены статуса на Rejected
    /// </summary>
    [Fact]
    public void Reject_ChangesStatusToRejected()
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending
        };
        
        booking.Status = BookingStatus.Rejected;
        booking.ProcessedAt = DateTime.UtcNow;
        
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
}