namespace Contracts.Messages;

/// <summary>
/// Бронь направлена на подтвержление
/// </summary>
public sealed record BookingRequested(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime RequestedAtUtc
);