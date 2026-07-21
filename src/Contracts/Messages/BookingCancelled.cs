namespace Contracts.Messages;

/// <summary>
/// Бронь отменена пользователем
/// </summary>
public sealed record BookingCancelled(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime CancelledAtUtc
);