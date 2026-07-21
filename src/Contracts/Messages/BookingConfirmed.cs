namespace Contracts.Messages;

/// <summary>
/// Бронь подтверждена
/// </summary>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime ConfirmedAtUtc
);